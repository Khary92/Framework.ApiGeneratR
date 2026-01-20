using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.Generators.Helpers;

public static class InterfaceSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> GetInterfaceImplementingClassSymbols(
        this IncrementalGeneratorInitializationContext context, string attributeName)
    {
        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null);

        return handlerDeclarations.Collect();
    }

    private static INamedTypeSymbol? GetSemanticTarget(GeneratorSyntaxContext ctx)
    {
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node) as INamedTypeSymbol;

        return classSymbol != null && classSymbol.AllInterfaces.Any(i => i.Name == "IRequestHandler")
            ? classSymbol
            : null;
    }
}