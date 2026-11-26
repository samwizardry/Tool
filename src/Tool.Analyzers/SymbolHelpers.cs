using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tool.Analyzers;

internal static class SymbolHelpers
{
    public const int SizeThresholdBytes = 16;

    public static INamedTypeSymbol? TryGetAnalyzableStruct(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol candidate &&
            candidate.IsValueType &&
            candidate.TypeKind == TypeKind.Struct &&
            candidate.SpecialType == SpecialType.None &&
            !candidate.IsRefLikeType &&
            candidate.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return candidate;
        }

        return null;
    }

    public static bool IsPositionalRecordPrimaryConstructor(IMethodSymbol method, CancellationToken cancellationToken)
    {
        return method.MethodKind == MethodKind.Constructor &&
               method.DeclaringSyntaxReferences.Length > 0 &&
               method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not ConstructorDeclarationSyntax;
    }

    public static bool IsInterfaceImplementation(IMethodSymbol method)
    {
        if (method.ExplicitInterfaceImplementations.Length > 0)
        {
            return true;
        }

        if (method.ContainingType is not { } containingType)
        {
            return false;
        }

        foreach (INamedTypeSymbol iface in containingType.AllInterfaces)
        {
            foreach (ISymbol member in iface.GetMembers(method.Name))
            {
                if (SymbolEqualityComparer.Default.Equals(
                    containingType.FindImplementationForInterfaceMember(member), method))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static Location GetPrimaryLocation(ISymbol symbol) =>
        symbol.Locations.Length > 0 ? symbol.Locations[0] : Location.None;
}
