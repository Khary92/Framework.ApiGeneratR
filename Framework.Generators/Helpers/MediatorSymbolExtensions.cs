using System.Collections.Immutable;
using Framework.Generators.Generators.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.Generators.Helpers;

public static class MediatorSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<RequestHandlerSourceData>> GetRequestHandlerClassSymbols(
        this IncrementalGeneratorInitializationContext context)
    {
        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null);

        var handlerData = handlerDeclarations.Select(static (symbol, _) =>
        {
            if (symbol is null) return null;

            var interfaceSymbol = symbol.AllInterfaces.FirstOrDefault(i =>
                i.Name == "IRequestHandler" &&
                i.ContainingNamespace.ToDisplayString().Contains("Framework.Contract"));

            if (interfaceSymbol is null || interfaceSymbol.TypeArguments.Length != 2)
                return null;

            var requestType = interfaceSymbol.TypeArguments[0]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var requestShortName = interfaceSymbol.TypeArguments[0].Name;

            var responseType = interfaceSymbol.TypeArguments[1]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var responseShortName = interfaceSymbol.TypeArguments[1].Name;

            var handlerType = symbol
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            return new RequestHandlerSourceData(symbol.Name, requestType, requestShortName, responseShortName,
                responseType, handlerType);
        });

        return handlerData
            .Where(static d => d is not null)
            .Select(static (d, _) => d!)
            .Collect();
    }

    private static INamedTypeSymbol? GetSemanticTarget(GeneratorSyntaxContext ctx)
    {
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node) as INamedTypeSymbol;

        return classSymbol != null && classSymbol.AllInterfaces.Any(i => i.Name == "IRequestHandler")
            ? classSymbol
            : null;
    }
}