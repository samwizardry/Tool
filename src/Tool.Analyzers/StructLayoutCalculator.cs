using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;

namespace Tool.Analyzers;

/// <summary>
/// Compilation-scoped, thread-safe estimator of x64 CLR struct sizes, alignment, and padding.
/// Reads exact PE metadata for external compiled assemblies and reports unknown layouts explicitly.
/// </summary>
internal sealed class StructLayoutCalculator(Compilation compilation)
{
    private readonly INamedTypeSymbol? _inlineArrayAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");
    private readonly INamedTypeSymbol? _structLayoutAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
    private readonly INamedTypeSymbol? _extendedLayoutAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ExtendedLayoutAttribute");
    private readonly INamedTypeSymbol? _fieldOffsetAttribute =
        compilation.GetTypeByMetadataName("System.Runtime.InteropServices.FieldOffsetAttribute");
    private readonly INamedTypeSymbol? _keyValuePairType =
        compilation.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2");
    private readonly INamedTypeSymbol? _valueTaskGenericType =
        compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
    private readonly Dictionary<ISymbol, StructLayoutInfo> _knownLayouts = BuildKnownLayouts(compilation);
    private readonly MetadataLayoutReader _metadataReader = new();
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

    public bool HasExplicitSourceLayout(INamedTypeSymbol type)
    {
        if (TryGetSourceStructLayout(type, out _))
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
                TryGetSourceFieldOffset(field, out _))
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

        INamedTypeSymbol originalDef = namedType.OriginalDefinition;

        if (originalDef.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            return ComputeNullableLayout(namedType.TypeArguments[0], visited);
        }

        if (_keyValuePairType is not null &&
            SymbolEqualityComparer.Default.Equals(originalDef, _keyValuePairType) &&
            namedType.TypeArguments.Length == 2)
        {
            return ComputeKeyValuePairLayout(namedType.TypeArguments[0], namedType.TypeArguments[1], visited);
        }

        if (_valueTaskGenericType is not null &&
            SymbolEqualityComparer.Default.Equals(originalDef, _valueTaskGenericType) &&
            namedType.TypeArguments.Length == 1)
        {
            return ComputeValueTaskLayout(namedType.TypeArguments[0], visited);
        }

        if (_knownLayouts.TryGetValue(originalDef, out StructLayoutInfo known))
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

        // Nullable<T> holds 'bool hasValue' (1 byte) + 'T value' with generic Auto layout.
        int size = AlignUp(elem.Size + 1, elem.Alignment);
        return StructLayoutInfo.Known(size, size, elem.Alignment, elem.HasReferences);
    }

    private StructLayoutInfo ComputeKeyValuePairLayout(
        ITypeSymbol keyType,
        ITypeSymbol valueType,
        HashSet<ISymbol> visited)
    {
        StructLayoutInfo key = ComputeLayout(keyType, visited);
        if (!key.IsKnown)
        {
            return key;
        }

        StructLayoutInfo val = ComputeLayout(valueType, visited);
        if (!val.IsKnown)
        {
            return val;
        }

        int maxAlign = Math.Max(key.Alignment, val.Alignment);
        int size = AlignUp(key.Size + val.Size, maxAlign);
        return StructLayoutInfo.Known(size, size, maxAlign, key.HasReferences || val.HasReferences);
    }

    private StructLayoutInfo ComputeValueTaskLayout(ITypeSymbol resultType, HashSet<ISymbol> visited)
    {
        StructLayoutInfo res = ComputeLayout(resultType, visited);
        if (!res.IsKnown)
        {
            return res;
        }

        // ValueTask<TResult>: object? _obj (8B) + TResult _result + short _token (2B) + bool _continueOnCapturedContext (1B).
        int maxAlign = Math.Max(8, res.Alignment);
        int size = AlignUp(8 + res.Size + 2 + 1, maxAlign);
        return StructLayoutInfo.Known(size, size, maxAlign, hasReferences: true);
    }

    private StructLayoutInfo ComputeStructLayout(INamedTypeSymbol namedType, HashSet<ISymbol> visited)
    {
        bool isInSource = namedType.Locations.Length > 0 && namedType.Locations[0].IsInSource;

        RawTypeLayout rawLayout;
        if (isInSource)
        {
            if (!TryGetSourceStructLayout(namedType, out rawLayout))
            {
                rawLayout = new RawTypeLayout(MetadataLayoutKind.Sequential, pack: 0, size: 0, fieldOffsets: null);
            }
        }
        else if (!_metadataReader.TryGetTypeLayout(namedType, out rawLayout))
        {
            // External metadata could not be read; report unknown instead of guessing layout/pack/offsets.
            return StructLayoutInfo.Unknown(namedType);
        }

        if (rawLayout.Kind == MetadataLayoutKind.Extended)
        {
            return StructLayoutInfo.Unknown(namedType);
        }

        bool isExplicit = rawLayout.Kind == MetadataLayoutKind.Explicit;
        int pack = rawLayout.Pack;
        int explicitSize = rawLayout.Size;

        int sequentialOffset = 0;
        int explicitMaxEnd = 0;
        int sumFieldSizes = 0;
        int maxAlign = 1;
        bool hasReferences = false;
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

            int effectiveAlign = pack > 0
                ? Math.Min(fieldLayout.Alignment, pack)
                : fieldLayout.Alignment;

            if (isExplicit)
            {
                if (!TryGetFieldOffset(field, isInSource, rawLayout, out int fieldOffset))
                {
                    return StructLayoutInfo.Unknown(namedType);
                }

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

            // Empty C# structs occupy 1 byte in the CLR; 0-field metadata structs with no Size may be stripped stubs.
            return isInSource
                ? StructLayoutInfo.Known(1, 1, 1, hasReferences: false)
                : StructLayoutInfo.Unknown(namedType);
        }

        int actualSize;
        int optimalSize;

        if (isExplicit)
        {
            actualSize = AlignUp(explicitMaxEnd, maxAlign);
            optimalSize = actualSize;
        }
        else
        {
            optimalSize = AlignUp(sumFieldSizes, maxAlign);
            int sequentialSize = AlignUp(sequentialOffset, maxAlign);

            // CoreCLR uses Auto layout for structs with GC references, generic structs, or LayoutKind.Auto.
            bool isAutoLayout = hasReferences || namedType.IsGenericType || rawLayout.Kind == MetadataLayoutKind.Auto;
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
        // C# 11 ref/ref readonly fields inside ref structs store a managed pointer (8 bytes on x64).
        if (field.RefKind != RefKind.None)
        {
            return StructLayoutInfo.Known(8, 8, 8, hasReferences: true);
        }

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

    private bool TryGetFieldOffset(
        IFieldSymbol field,
        bool isInSource,
        RawTypeLayout rawLayout,
        out int offset)
    {
        if (isInSource)
        {
            return TryGetSourceFieldOffset(field, out offset);
        }

        if (rawLayout.FieldOffsets is not null &&
            rawLayout.FieldOffsets.TryGetValue(field.MetadataName, out offset))
        {
            return true;
        }

        offset = 0;
        return false;
    }

    private bool TryGetSourceFieldOffset(IFieldSymbol field, out int offset)
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

    private bool TryGetSourceStructLayout(INamedTypeSymbol type, out RawTypeLayout layout)
    {
        foreach (AttributeData attr in type.GetAttributes())
        {
            if (_extendedLayoutAttribute is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _extendedLayoutAttribute))
            {
                layout = new RawTypeLayout(MetadataLayoutKind.Extended, pack: 0, size: 0, fieldOffsets: null);
                return true;
            }

            if (_structLayoutAttribute is null ||
                !SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _structLayoutAttribute))
            {
                continue;
            }

            MetadataLayoutKind kind = MetadataLayoutKind.Sequential;
            if (attr.ConstructorArguments.Length > 0)
            {
                object? rawArg = attr.ConstructorArguments[0].Value;
                int rawKind = rawArg is int intVal ? intVal : rawArg is short shortVal ? shortVal : -1;
                kind = (LayoutKind)rawKind switch
                {
                    LayoutKind.Sequential => MetadataLayoutKind.Sequential,
                    LayoutKind.Explicit => MetadataLayoutKind.Explicit,
                    LayoutKind.Auto => MetadataLayoutKind.Auto,
                    _ => MetadataLayoutKind.Extended
                };
            }

            int pack = 0;
            int size = 0;
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

            layout = new RawTypeLayout(kind, pack, size, fieldOffsets: null);
            return true;
        }

        layout = default;
        return false;
    }

    private static Dictionary<ISymbol, StructLayoutInfo> BuildKnownLayouts(Compilation compilation)
    {
        var map = new Dictionary<ISymbol, StructLayoutInfo>(SymbolEqualityComparer.Default);

        // Core .NET BCL value types whose private fields are stripped in Microsoft.NETCore.App.Ref.
        Add(compilation, map, "System.ValueTuple", 1, 1);
        Add(compilation, map, "System.Half", 2, 2);
        Add(compilation, map, "System.Index", 4, 4);
        Add(compilation, map, "System.DateOnly", 4, 4);
        Add(compilation, map, "System.Text.Rune", 4, 4);
        Add(compilation, map, "System.TimeSpan", 8, 8);
        Add(compilation, map, "System.DateTime", 8, 8);
        Add(compilation, map, "System.TimeOnly", 8, 8);
        Add(compilation, map, "System.Range", 8, 4);
        Add(compilation, map, "System.Runtime.InteropServices.NFloat", 8, 8);
        Add(compilation, map, "System.Runtime.InteropServices.GCHandle", 8, 8);
        Add(compilation, map, "System.Threading.CancellationToken", 8, 8, hasReferences: true);
        Add(compilation, map, "System.RuntimeTypeHandle", 8, 8, hasReferences: true);
        Add(compilation, map, "System.RuntimeFieldHandle", 8, 8, hasReferences: true);
        Add(compilation, map, "System.RuntimeMethodHandle", 8, 8, hasReferences: true);
        Add(compilation, map, "System.Guid", 16, 4);
        Add(compilation, map, "System.Int128", 16, 8);
        Add(compilation, map, "System.UInt128", 16, 8);
        Add(compilation, map, "System.DateTimeOffset", 16, 8);
        Add(compilation, map, "System.Threading.Tasks.ValueTask", 16, 8, hasReferences: true);
        Add(compilation, map, "System.Memory`1", 16, 8, hasReferences: true);
        Add(compilation, map, "System.ReadOnlyMemory`1", 16, 8, hasReferences: true);
        Add(compilation, map, "System.ArraySegment`1", 16, 8, hasReferences: true);
        Add(compilation, map, "System.Span`1", 16, 8, hasReferences: true);
        Add(compilation, map, "System.ReadOnlySpan`1", 16, 8, hasReferences: true);
        Add(compilation, map, "System.HashCode", 32, 4);

        // Hardware SIMD intrinsic vectors (reference assemblies omit ClassLayout.Size and private fields).
        Add(compilation, map, "System.Runtime.Intrinsics.Vector64`1", 8, 8);
        Add(compilation, map, "System.Runtime.Intrinsics.Vector128`1", 16, 8);
        Add(compilation, map, "System.Runtime.Intrinsics.Vector256`1", 32, 8);
        Add(compilation, map, "System.Runtime.Intrinsics.Vector512`1", 64, 8);

        // System.Numerics fixed-size math types (System.Numerics.Vector<T> is intentionally omitted: CPU-ISA-dependent).
        Add(compilation, map, "System.Numerics.Vector2", 8, 4);
        Add(compilation, map, "System.Numerics.Vector3", 12, 4);
        Add(compilation, map, "System.Numerics.Vector4", 16, 4);
        Add(compilation, map, "System.Numerics.Quaternion", 16, 4);
        Add(compilation, map, "System.Numerics.Plane", 16, 4);
        Add(compilation, map, "System.Numerics.Matrix3x2", 24, 4);
        Add(compilation, map, "System.Numerics.Matrix4x4", 64, 4);
        Add(compilation, map, "System.Numerics.Complex", 16, 8);

        // System.Drawing.Primitives structs (private fields stripped in ref assemblies).
        Add(compilation, map, "System.Drawing.Point", 8, 4);
        Add(compilation, map, "System.Drawing.PointF", 8, 4);
        Add(compilation, map, "System.Drawing.Size", 8, 4);
        Add(compilation, map, "System.Drawing.SizeF", 8, 4);
        Add(compilation, map, "System.Drawing.Rectangle", 16, 4);
        Add(compilation, map, "System.Drawing.RectangleF", 16, 4);
        Add(compilation, map, "System.Drawing.Color", 24, 8, hasReferences: true);

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

        SpecialType.System_Decimal => StructLayoutInfo.Known(16, 16, 8, false),

        _ => StructLayoutInfo.GenericOrUnresolved
    };

    private static int AlignUp(int value, int alignment) =>
        (value + alignment - 1) & ~(alignment - 1);
}
