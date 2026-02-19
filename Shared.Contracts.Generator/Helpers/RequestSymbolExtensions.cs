using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shared.Contract.Generator.Mapper;

namespace Shared.Contract.Generator.Helpers;

public static class RequestSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<RequestData>> GetRequestSourceData(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                "Shared.Contracts.Attributes.RequestAttribute",
                (node, _) =>
                    node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var typeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

                    var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(i =>
                        i.Name == "IRequest" &&
                        i is { IsGenericType: true, TypeArguments.Length: 1 } &&
                        i.ContainingNamespace.ToDisplayString().StartsWith("Shared.Contracts.Mediator"));

                    if (interfaceSymbol is null) return null;

                    var returnValueFullName = interfaceSymbol.TypeArguments[0];

                    var attribute = ctx.Attributes.FirstOrDefault();
                    if (attribute == null) return null;

                    var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    var arrayBuilder = ImmutableArray.CreateBuilder<string>();
                    foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
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

                    var hasIdentityId = typeSymbol.GetMembers().Any(m => m.Name == "IdentityId");
                    var type = typeSymbol.IsRecord ? "Record" : "Class";

                    return new RequestData(
                        route,
                        requiresAuth,
                        hasIdentityId,
                        httpMethod,
                        cqsType,
                        typeSymbol.Name,
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