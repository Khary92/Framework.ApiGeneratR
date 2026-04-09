using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.Helpers.Extractors.Api;

public static class ApiEnumExtractor
{
    public static IncrementalValueProvider<ImmutableArray<ApiEnumData>> GetApiEnumData(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                "ApiGeneratR.Attributes.ApiEnumAttribute",
                (node, _) => node is EnumDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                
                    var fields = symbol.GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(f => f.HasConstantValue)
                        .Select(f => f.Name)
                        .ToImmutableArray();

                    return new ApiEnumData(symbol.Name, fields);
                })
            .Collect();
    }
}