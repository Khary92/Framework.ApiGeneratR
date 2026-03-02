using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.CodeGen.Helpers;

public static class RequestSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<RequestData>> GetRequestSourceData(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                "ApiGeneratR.Attributes.RequestAttribute",
                (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var attribute = ctx.Attributes.FirstOrDefault();

                    if (attribute is null) return null;

                    var requestInterface = symbol.AllInterfaces
                        .FirstOrDefault(i => i.Name == "IRequest" && i.TypeArguments.Length == 1);

                    if (requestInterface is null) return null;

                    var returnValueFullName = requestInterface.TypeArguments[0];

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
                        ? (int)(attribute.ConstructorArguments[2].Value ?? 0)
                        : 0;

                    var cqsType = requestValue switch
                    {
                        0 => "Query",
                        1 => "Command",
                        _ => "Unknown"
                    };

                    var methodValue = attribute.ConstructorArguments.Length > 3
                        ? (int)(attribute.ConstructorArguments[3].Value ?? 1)
                        : 1;

                    var httpMethod = methodValue switch
                    {
                        0 => "Get",
                        1 => "Post",
                        2 => "Put",
                        3 => "Delete",
                        4 => "Patch",
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