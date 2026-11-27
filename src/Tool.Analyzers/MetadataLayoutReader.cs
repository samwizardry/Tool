using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Tool.Analyzers;

internal enum MetadataLayoutKind
{
    Sequential = 0,
    Explicit = 2,
    Auto = 3,
    Extended = 4
}

internal readonly struct RawTypeLayout(
    MetadataLayoutKind kind,
    int pack,
    int size,
    IReadOnlyDictionary<string, int>? fieldOffsets)
{
    public readonly MetadataLayoutKind Kind = kind;
    public readonly int Pack = pack;
    public readonly int Size = size;
    public readonly IReadOnlyDictionary<string, int>? FieldOffsets = fieldOffsets;
}

/// <summary>
/// Reads ECMA-335 struct layout metadata (LayoutMask, ClassLayout, FieldLayout) directly from
/// Roslyn's in-memory <see cref="ModuleMetadata"/> for compiled external assemblies.
/// </summary>
internal sealed class MetadataLayoutReader
{
    private readonly ConcurrentDictionary<ISymbol, Dictionary<string, RawTypeLayout>?> _moduleCache =
        new(SymbolEqualityComparer.Default);

    public bool TryGetTypeLayout(INamedTypeSymbol type, out RawTypeLayout layout)
    {
        IModuleSymbol? module = type.ContainingModule;
        if (module is null)
        {
            layout = default;
            return false;
        }

        Dictionary<string, RawTypeLayout>? moduleTypes = _moduleCache.GetOrAdd(module, static m => BuildModuleIndex((IModuleSymbol)m));
        if (moduleTypes is null)
        {
            layout = default;
            return false;
        }

        string key = GetMetadataFullName(type.OriginalDefinition);
        return moduleTypes.TryGetValue(key, out layout);
    }

    private static Dictionary<string, RawTypeLayout>? BuildModuleIndex(IModuleSymbol module)
    {
        // Do not dispose the returned ModuleMetadata; it is owned by the Compilation.
        if (module.GetMetadata() is not { } moduleMetadata)
        {
            return null;
        }

        MetadataReader reader = moduleMetadata.GetMetadataReader();
        var types = new Dictionary<string, RawTypeLayout>(StringComparer.Ordinal);
        var nameCache = new Dictionary<TypeDefinitionHandle, string>();

        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);
            if ((typeDef.Attributes & TypeAttributes.Interface) != 0)
            {
                continue;
            }

            MetadataLayoutKind kind = (typeDef.Attributes & TypeAttributes.LayoutMask) switch
            {
                TypeAttributes.AutoLayout => MetadataLayoutKind.Auto,
                TypeAttributes.SequentialLayout => MetadataLayoutKind.Sequential,
                TypeAttributes.ExplicitLayout => MetadataLayoutKind.Explicit,
                _ => MetadataLayoutKind.Extended
            };

            TypeLayout peLayout = typeDef.GetLayout();
            int pack = peLayout.IsDefault ? 0 : peLayout.PackingSize;
            int size = peLayout.IsDefault ? 0 : peLayout.Size;

            Dictionary<string, int>? fieldOffsets = null;
            if (kind == MetadataLayoutKind.Explicit)
            {
                foreach (FieldDefinitionHandle fieldHandle in typeDef.GetFields())
                {
                    FieldDefinition fieldDef = reader.GetFieldDefinition(fieldHandle);
                    int offset = fieldDef.GetOffset();
                    if (offset >= 0)
                    {
                        fieldOffsets ??= new Dictionary<string, int>(StringComparer.Ordinal);
                        fieldOffsets[reader.GetString(fieldDef.Name)] = offset;
                    }
                }
            }

            string fullName = GetTypeDefFullName(reader, handle, nameCache);
            types[fullName] = new RawTypeLayout(kind, pack, size, fieldOffsets);
        }

        return types;
    }

    private static string GetTypeDefFullName(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        Dictionary<TypeDefinitionHandle, string> cache)
    {
        if (cache.TryGetValue(handle, out string cached))
        {
            return cached;
        }

        TypeDefinition typeDef = reader.GetTypeDefinition(handle);
        string name = reader.GetString(typeDef.Name);
        TypeDefinitionHandle declaringHandle = typeDef.GetDeclaringType();

        string fullName;
        if (!declaringHandle.IsNil)
        {
            fullName = GetTypeDefFullName(reader, declaringHandle, cache) + "+" + name;
        }
        else
        {
            string ns = reader.GetString(typeDef.Namespace);
            fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        cache[handle] = fullName;
        return fullName;
    }

    private static string GetMetadataFullName(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();
        AppendTypeName(type, sb);
        return sb.ToString();
    }

    private static void AppendTypeName(INamedTypeSymbol type, StringBuilder sb)
    {
        if (type.ContainingType is not null)
        {
            AppendTypeName(type.ContainingType, sb);
            sb.Append('+');
            sb.Append(type.MetadataName);
            return;
        }

        AppendNamespace(type.ContainingNamespace, sb);
        if (sb.Length > 0)
        {
            sb.Append('.');
        }

        sb.Append(type.MetadataName);
    }

    private static void AppendNamespace(INamespaceSymbol? ns, StringBuilder sb)
    {
        if (ns is null || ns.IsGlobalNamespace)
        {
            return;
        }

        AppendNamespace(ns.ContainingNamespace, sb);
        if (sb.Length > 0)
        {
            sb.Append('.');
        }

        sb.Append(ns.MetadataName);
    }
}
