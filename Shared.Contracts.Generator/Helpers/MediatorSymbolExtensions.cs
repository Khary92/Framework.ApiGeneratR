using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shared.Contract.Generator.Mapper;

namespace Shared.Contract.Generator.Helpers;

public static class MediatorSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<RequestHandlerSourceData>> GetRequestHandlerClassSymbols(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                (ctx, _) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) is not INamedTypeSymbol
                        {
                            TypeKind: TypeKind.Class
                        } typeSymbol) return null;

                    var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(i =>
                        i.Name == "IRequestHandler" &&
                        i is { IsGenericType: true, TypeArguments.Length: 2 } &&
                        i.ContainingNamespace.ToDisplayString().StartsWith("Shared.Contracts"));

                    if (interfaceSymbol is null) return null;

                    var requestSymbol = interfaceSymbol.TypeArguments[0];
                    var responseSymbol = interfaceSymbol.TypeArguments[1];

                    return new RequestHandlerSourceData(
                        typeSymbol.Name,
                        requestSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        requestSymbol.Name,
                        responseSymbol.Name,
                        responseSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                })
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }
}