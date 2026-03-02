using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shared.Contract.Generator.Mapper;

namespace Shared.Contract.Generator.Helpers;

public static class RequestSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<RequestData>> GetRequestSourceData(
        this IncrementalGeneratorInitializationContext context, string nameSpace)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{nameSpace}.Generated.RequestAttribute",
                (node, _) =>
                    node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var attribute = ctx.Attributes.FirstOrDefault();

                    if (attribute is null) return null;
                    if (symbol.TypeArguments.Length == 0) return null;

                    var returnValueFullName = symbol.TypeArguments[0];

                    var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    var arrayBuilder = ImmutableArray.CreateBuilder<string>();
                    foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (member.DeclaredAccessibility != Accessibility.Public) continue;
                        if (member.IsStatic || member.IsIndexer) continue;

                        arrayBuilder.Add($"public {member.Type.ToDisplayString()} {member.Name} {{ get; }}");
                    }

                    var route = attribute.ConstructorArguments.Length > 0
                        ? attribute.ConstructorArguments[0].Value?.ToString() ?? "/unknown"
                        : "/unknown";

                    var requiresAuth = attribute.ConstructorArguments.Length > 1
                                       && attribute.ConstructorArguments[1].Value is true;

                    var requestValue = attribute.ConstructorArguments.Length > 2
                        ? attribute.ConstructorArguments[2].Value?.ToString()
                        : "0";

                    var cqsType = requestValue switch
                    {
                        "0" => "Command",
                        "1" => "Query",
                        _ => "Unknown"
                    };

                    var methodValue = attribute.ConstructorArguments.Length > 3
                        ? attribute.ConstructorArguments[3].Value?.ToString()
                        : "1";

                    var httpMethod = methodValue switch
                    {
                        "0" => "Get",
                        "1" => "Post",
                        "2" => "Put",
                        "3" => "Delete",
                        "4" => "Patch",
                        _ => "Post"
                    };

                    var hasIdentityId = symbol.GetMembers().Any(m => m.Name == "IdentityId");
                    var type = symbol.IsRecord ? "Record" : "Class";

                    return new RequestData(
                        route,
                        requiresAuth,
                        hasIdentityId,
                        httpMethod,
                        cqsType,
                        symbol.Name,
                        fullName,
                        returnValueFullName.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        arrayBuilder.ToImmutable(),
                        type);
                })
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }
}