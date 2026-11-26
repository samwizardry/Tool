using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Tool.Analyzers;

/// <summary>
/// Compilation-scoped, thread-safe estimator of x64 CLR struct sizes, alignment, and padding.
/// </summary>
internal sealed class StructLayoutCalculator(Compilation compilation)
{
    private const int LayoutKindSequential = 0;
    private const int LayoutKindExplicit = 2;
    private const int LayoutKindAuto = 3;

    private readonly INamedTypeSymbol? _inlineArrayAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");
    private readonly INamedTypeSymbol? _structLayoutAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
    private readonly INamedTypeSymbol? _fieldOffsetAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.InteropServices.FieldOffsetAttribute");
    private readonly Dictionary<ISymbol, StructLayoutInfo> _knownLayouts = BuildKnownLayouts(compilation);
    private readonly ConcurrentDictionary<ISymbol, StructLayoutInfo> _cache = new(SymbolEqualityComparer.Default);

    public StructLayoutInfo GetLayout(ITypeSymbol type)
    {
        if (_cache.TryGetValue(type, out StructLayoutInfo cached))
        {
            return cached;
        }

        var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        return ComputeLayout(type, visited);
    }

    public bool HasExplicitStructLayout(INamedTypeSymbol type)
    {
        if (TryGetStructLayout(type, out _, out _, out _))
        {
            return true;
        }

        if (_fieldOffsetAttribute is null)
        {
            return false;
        }

        foreach (ISymbol member in type.GetMembers())
        {
            if (member is IFieldSymbol { IsStatic: false, IsConst: false } field &&
                TryGetFieldOffset(field, out _))
            {
                return true;
            }
        }

        return false;
    }

    public int GetInlineArrayLength(INamedTypeSymbol type)
    {
        if (_inlineArrayAttribute is null)
        {
            return 0;
        }

        foreach (AttributeData attr in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _inlineArrayAttribute) &&
                attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is int length &&
                length > 0)
            {
                return length;
            }
        }

        return 0;
    }

    private StructLayoutInfo ComputeLayout(ITypeSymbol type, HashSet<ISymbol> visited)
    {
        if (_cache.TryGetValue(type, out StructLayoutInfo cached))
        {
            return cached;
        }

        if (type.TypeKind is TypeKind.TypeParameter or TypeKind.Error)
        {
            return StructLayoutInfo.GenericOrUnresolved;
        }

        if (type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer)
        {
            return StructLayoutInfo.Known(8, 8, 8, hasReferences: false);
        }

        if (!type.IsValueType)
        {
            return StructLayoutInfo.Known(8, 8, 8, hasReferences: true);
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType && enumType.EnumUnderlyingType is not null)
        {
            return ComputePrimitiveLayout(enumType.EnumUnderlyingType.SpecialType);
        }

        StructLayoutInfo primitive = ComputePrimitiveLayout(type.SpecialType);
        if (primitive.IsKnown)
        {
            return primitive;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return StructLayoutInfo.Unknown(type);
        }

        if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            return ComputeNullableLayout(namedType.TypeArguments[0], visited);
        }

        if (_knownLayouts.TryGetValue(namedType.OriginalDefinition, out StructLayoutInfo known))
        {
            return known;
        }

        if (!visited.Add(namedType))
        {
            return StructLayoutInfo.GenericOrUnresolved;
        }

        StructLayoutInfo result = ComputeStructLayout(namedType, visited);
        visited.Remove(namedType);

        _cache.TryAdd(namedType, result);
        return result;
    }

    private StructLayoutInfo ComputeNullableLayout(ITypeSymbol underlyingType, HashSet<ISymbol> visited)
    {
        StructLayoutInfo elem = ComputeLayout(underlyingType, visited);
        if (!elem.IsKnown)
        {
            return elem;
        }

        // Nullable<T> layout: 'bool hasValue' (1 byte) + 'T value' (auto-packed if T contains GC references).
        int size = elem.HasReferences
            ? AlignUp(elem.Size + 1, elem.Alignment)
            : AlignUp(AlignUp(1, elem.Alignment) + elem.Size, elem.Alignment);

        return StructLayoutInfo.Known(size, size, elem.Alignment, elem.HasReferences);
    }

    private StructLayoutInfo ComputeStructLayout(INamedTypeSymbol namedType, HashSet<ISymbol> visited)
    {
        bool hasExplicitLayout = TryGetStructLayout(namedType, out int layoutKind, out int pack, out int explicitSize);
        bool isExplicit = hasExplicitLayout && layoutKind == LayoutKindExplicit;

        int sequentialOffset = 0;
        int explicitMaxEnd = 0;
        int sumFieldSizes = 0;
        int maxAlign = 1;
        bool hasReferences = false;
        bool hasAnyFieldOffset = false;
        int fieldCount = 0;

        foreach (ISymbol member in namedType.GetMembers())
        {
            if (member is not IFieldSymbol { IsStatic: false, IsConst: false } field)
            {
                continue;
            }

            // Reference assemblies (GenAPI) replace stripped private fields with synthetic '_dummy' / '_dummyPrimitive'.
            if (field.Name is "_dummy" or "_dummyPrimitive")
            {
                return StructLayoutInfo.Unknown(namedType);
            }

            StructLayoutInfo fieldLayout = ComputeFieldLayout(field, visited);
            if (!fieldLayout.IsKnown)
            {
                return fieldLayout.IsUnknownLayout
                    ? StructLayoutInfo.Unknown(fieldLayout.UnknownType ?? field.Type)
                    : StructLayoutInfo.GenericOrUnresolved;
            }

            int effectiveAlign = (hasExplicitLayout && pack > 0)
                ? Math.Min(fieldLayout.Alignment, pack)
                : fieldLayout.Alignment;

            if (TryGetFieldOffset(field, out int fieldOffset))
            {
                hasAnyFieldOffset = true;
                int fieldEnd = fieldOffset + fieldLayout.Size;
                if (fieldEnd > explicitMaxEnd)
                {
                    explicitMaxEnd = fieldEnd;
                }
            }

            sequentialOffset = AlignUp(sequentialOffset, effectiveAlign) + fieldLayout.Size;
            sumFieldSizes += fieldLayout.Size;
            if (effectiveAlign > maxAlign)
            {
                maxAlign = effectiveAlign;
            }

            hasReferences |= fieldLayout.HasReferences;
            fieldCount++;
        }

        if (fieldCount == 0)
        {
            if (explicitSize > 0)
            {
                return StructLayoutInfo.Known(explicitSize, explicitSize, 1, hasReferences: false);
            }

            // Empty source structs occupy 1 byte in the CLR; metadata-only 0-field structs may be stripped ref types.
            bool isInSource = namedType.Locations.Length > 0 && namedType.Locations[0].IsInSource;
            return isInSource
                ? StructLayoutInfo.Known(1, 1, 1, hasReferences: false)
                : StructLayoutInfo.Unknown(namedType);
        }

        int actualSize;
        int optimalSize;

        if (isExplicit || hasAnyFieldOffset)
        {
            if (hasAnyFieldOffset)
            {
                actualSize = AlignUp(explicitMaxEnd, maxAlign);
                optimalSize = actualSize;
            }
            else if (explicitSize > 0)
            {
                actualSize = explicitSize;
                optimalSize = explicitSize;
            }
            else
            {
                return StructLayoutInfo.Unknown(namedType);
            }
        }
        else
        {
            optimalSize = AlignUp(sumFieldSizes, maxAlign);
            int sequentialSize = AlignUp(sequentialOffset, maxAlign);

            // CLR promotes structs containing GC references (or marked LayoutKind.Auto) to automatic packing.
            bool isAutoLayout = hasReferences || (hasExplicitLayout && layoutKind == LayoutKindAuto);
            actualSize = isAutoLayout ? optimalSize : sequentialSize;
        }

        if (explicitSize > actualSize)
        {
            actualSize = explicitSize;
            optimalSize = explicitSize;
        }

        int inlineLength = GetInlineArrayLength(namedType);
        if (inlineLength > 0)
        {
            actualSize *= inlineLength;
            optimalSize *= inlineLength;
        }

        return StructLayoutInfo.Known(actualSize, optimalSize, maxAlign, hasReferences);
    }

    private StructLayoutInfo ComputeFieldLayout(IFieldSymbol field, HashSet<ISymbol> visited)
    {
        if (field.IsFixedSizeBuffer && field.Type is IPointerTypeSymbol ptr)
        {
            StructLayoutInfo elem = ComputeLayout(ptr.PointedAtType, visited);
            if (!elem.IsKnown)
            {
                return elem;
            }

            int stride = AlignUp(elem.Size, elem.Alignment);
            int total = stride * field.FixedSize;
            return StructLayoutInfo.Known(total, total, elem.Alignment, elem.HasReferences);
        }

        return ComputeLayout(field.Type, visited);
    }

    private bool TryGetFieldOffset(IFieldSymbol field, out int offset)
    {
        offset = 0;
        if (_fieldOffsetAttribute is null)
        {
            return false;
        }

        foreach (AttributeData attr in field.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _fieldOffsetAttribute) &&
                attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is int value &&
                value >= 0)
            {
                offset = value;
                return true;
            }
        }

        return false;
    }

    private bool TryGetStructLayout(INamedTypeSymbol type, out int layoutKind, out int pack, out int size)
    {
        layoutKind = LayoutKindSequential;
        pack = 0;
        size = 0;

        if (_structLayoutAttribute is null)
        {
            return false;
        }

        foreach (AttributeData attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _structLayoutAttribute))
            {
                continue;
            }

            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int kind)
            {
                layoutKind = kind;
            }

            foreach (KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
            {
                if (named.Key == "Pack" && named.Value.Value is int p)
                {
                    pack = p;
                }
                else if (named.Key == "Size" && named.Value.Value is int s)
                {
                    size = s;
                }
            }

            return true;
        }

        return false;
    }

    private static Dictionary<ISymbol, StructLayoutInfo> BuildKnownLayouts(Compilation compilation)
    {
        var map = new Dictionary<ISymbol, StructLayoutInfo>(SymbolEqualityComparer.Default);

        // Core BCL value types whose private fields are stripped in .NET reference assemblies.
        Add(compilation, map, "System.Half", 2, 2);
        Add(compilation, map, "System.Index", 4, 4);
        Add(compilation, map, "System.DateOnly", 4, 4);
        Add(compilation, map, "System.TimeSpan", 8, 8);
        Add(compilation, map, "System.DateTime", 8, 8);
        Add(compilation, map, "System.TimeOnly", 8, 8);
        Add(compilation, map, "System.Range", 8, 4);
        Add(compilation, map, "System.Runtime.InteropServices.NFloat", 8, 8);
        Add(compilation, map, "System.Runtime.InteropServices.GCHandle", 8, 8);
        Add(compilation, map, "System.Threading.CancellationToken", 8, 8, hasReferences: true);
        Add(compilation, map, "System.Guid", 16, 4);
        Add(compilation, map, "System.Int128", 16, 8);
        Add(compilation, map, "System.UInt128", 16, 8);
        Add(compilation, map, "System.DateTimeOffset", 16, 8);

        // Hardware SIMD intrinsic vectors (reference assemblies omit pseudo-custom [StructLayout(Size = ...)]).
        Add(compilation, map, "System.Runtime.Intrinsics.Vector64`1", 8, 8);
        Add(compilation, map, "System.Runtime.Intrinsics.Vector128`1", 16, 8);
        Add(compilation, map, "System.Runtime.Intrinsics.Vector256`1", 32, 8);
        Add(compilation, map, "System.Runtime.Intrinsics.Vector512`1", 64, 8);

        // System.Numerics math types.
        Add(compilation, map, "System.Numerics.Vector2", 8, 4);
        Add(compilation, map, "System.Numerics.Vector3", 12, 4);
        Add(compilation, map, "System.Numerics.Vector4", 16, 4);
        Add(compilation, map, "System.Numerics.Quaternion", 16, 4);
        Add(compilation, map, "System.Numerics.Plane", 16, 4);
        Add(compilation, map, "System.Numerics.Matrix3x2", 24, 4);
        Add(compilation, map, "System.Numerics.Matrix4x4", 64, 4);
        Add(compilation, map, "System.Numerics.Complex", 16, 8);

        return map;

        static void Add(
            Compilation comp,
            Dictionary<ISymbol, StructLayoutInfo> target,
            string metadataName,
            int size,
            int align,
            bool hasReferences = false)
        {
            if (comp.GetTypeByMetadataName(metadataName) is { } symbol)
            {
                target[symbol] = StructLayoutInfo.Known(size, size, align, hasReferences);
            }
        }
    }

    private static StructLayoutInfo ComputePrimitiveLayout(SpecialType specialType) => specialType switch
    {
        SpecialType.System_Boolean or
        SpecialType.System_Byte or
        SpecialType.System_SByte => StructLayoutInfo.Known(1, 1, 1, false),

        SpecialType.System_Int16 or
        SpecialType.System_UInt16 or
        SpecialType.System_Char => StructLayoutInfo.Known(2, 2, 2, false),

        SpecialType.System_Int32 or
        SpecialType.System_UInt32 or
        SpecialType.System_Single => StructLayoutInfo.Known(4, 4, 4, false),

        SpecialType.System_Int64 or
        SpecialType.System_UInt64 or
        SpecialType.System_Double or
        SpecialType.System_IntPtr or
        SpecialType.System_UIntPtr => StructLayoutInfo.Known(8, 8, 8, false),

        SpecialType.System_Decimal => StructLayoutInfo.Known(16, 16, 4, false),

        _ => StructLayoutInfo.GenericOrUnresolved
    };

    private static int AlignUp(int value, int alignment) =>
        (value + alignment - 1) & ~(alignment - 1);
}
