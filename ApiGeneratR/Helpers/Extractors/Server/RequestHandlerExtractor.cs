using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.Helpers.Extractors.Server;

public static class RequestHandlerExtractor
{
    public static IncrementalValueProvider<ImmutableArray<RequestHandlerData>> GetRequestHandlerSourceData(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                "ApiGeneratR.Attributes.RequestHandlerAttribute",
                (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var handlerSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var attribute = ctx.Attributes.First();

                    var requestTypeArgument = attribute.ConstructorArguments.FirstOrDefault();

                    if (requestTypeArgument.Value is not INamedTypeSymbol requestSymbol)
                        return null;

                    var requestInterface = requestSymbol.AllInterfaces
                        .FirstOrDefault(i => i.Name == "RequestResponseTag" && i.IsGenericType);

                    if (requestInterface is null) return null;

                    var responseSymbol = requestInterface.TypeArguments.FirstOrDefault();

                    if (responseSymbol == null) return null;

                    return new RequestHandlerData(
                        handlerSymbol.Name,
                        handlerSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        requestSymbol.Name,
                        requestSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        responseSymbol.Name,
                        responseSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        handlerSymbol.ContainingNamespace?.ToDisplayString() ?? "UnknownNamespace"
                    );
                })
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }
}