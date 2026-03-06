using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.CodeGen.Helpers;

public static class ConsumerApiSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<ApiConsumerData>> GetConsumerApiSourceData(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                "ApiGeneratR.Attributes.ApiConsumerAttribute",
                (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var attribute = ctx.Attributes.FirstOrDefault();

                    if (attribute is null) return null;

                    var @namespace = symbol.ContainingNamespace?.ToDisplayString() ?? "UnknownNamespace";

                    if (attribute.ConstructorArguments.Length == 0) return null;

                    var arg = attribute.ConstructorArguments[0];


                    var eventTypeNames = arg.Values
                        .Select(v =>
                        {
                            if (v.Value is ITypeSymbol typeSymbol)
                            {
                                return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            }

                            return v.Value?.ToString() ?? "UnknownType";
                        })
                        .ToImmutableArray();

                    return new ApiConsumerData(
                        eventTypeNames,
                        @namespace,
                        symbol.Name);
                })
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }
}