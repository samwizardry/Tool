using System.Collections.Generic;
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
            !candidate.IsRefLikeType)
        {
            return candidate;
        }

        return null;
    }

    public static string FormatTypeName(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    public static string FormatParameterRefKind(RefKind refKind) => refKind switch
    {
        RefKind.Ref => "ref",
        RefKind.RefReadOnlyParameter => "ref readonly",
        RefKind.In => "in",
        _ => refKind.ToString()
    };

    public static string FormatLocalOrReturnRefKind(RefKind refKind) => refKind switch
    {
        RefKind.Ref => "ref",
        RefKind.RefReadOnly => "ref readonly",
        _ => refKind.ToString()
    };

    public static Diagnostic CreateUnknownLayoutDiagnostic(
        Location location,
        ITypeSymbol structType,
        StructLayoutInfo layout)
    {
        return Diagnostic.Create(
            DiagnosticDescriptors.UnknownStructLayout,
            location,
            FormatTypeName(structType),
            FormatTypeName(layout.UnknownType ?? structType));
    }

    public static bool IsPrimaryConstructor(IMethodSymbol method, CancellationToken cancellationToken)
    {
        return method.MethodKind == MethodKind.Constructor &&
               method.DeclaringSyntaxReferences.Length > 0 &&
               method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not ConstructorDeclarationSyntax;
    }

    public static bool IsIteratorMethod(IMethodSymbol method, CancellationToken cancellationToken)
    {
        if (!IsIteratorReturnType(method.ReturnType) || method.DeclaringSyntaxReferences.Length == 0)
        {
            return false;
        }

        SyntaxNode syntax = method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        BlockSyntax? body = syntax switch
        {
            MethodDeclarationSyntax m => m.Body,
            LocalFunctionStatementSyntax lf => lf.Body,
            AccessorDeclarationSyntax a => a.Body,
            _ => null
        };

        if (body is null)
        {
            return false;
        }

        var stack = new Stack<SyntaxNode>();
        stack.Push(body);

        while (stack.Count > 0)
        {
            SyntaxNode current = stack.Pop();
            if (current is YieldStatementSyntax)
            {
                return true;
            }

            foreach (SyntaxNode child in current.ChildNodes())
            {
                if (child is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
                {
                    continue;
                }

                stack.Push(child);
            }
        }

        return false;
    }

    private static bool IsIteratorReturnType(ITypeSymbol returnType)
    {
        if (returnType.SpecialType is SpecialType.System_Collections_IEnumerable or SpecialType.System_Collections_IEnumerator)
        {
            return true;
        }

        if (returnType is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.OriginalDefinition.SpecialType is
            SpecialType.System_Collections_Generic_IEnumerable_T or
            SpecialType.System_Collections_Generic_IEnumerator_T)
        {
            return true;
        }

        return named.MetadataName is "IAsyncEnumerable`1" or "IAsyncEnumerator`1" &&
               named.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic";
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
