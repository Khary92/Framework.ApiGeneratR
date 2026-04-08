using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.Helpers;

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
                        .FirstOrDefault(i => i.Name == "RequestResponseTag" && i.TypeArguments.Length == 1);

                    if (requestInterface is null) return null;

                    var returnValueFullName = requestInterface.TypeArguments[0];

                    var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    var fieldsBuilder = ImmutableArray.CreateBuilder<FieldData>();
                    foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (member.DeclaredAccessibility != Accessibility.Public) continue;
                        if (member.IsStatic || member.IsIndexer) continue;

                        fieldsBuilder.Add(new FieldData(member.Name, member.Type.ToDisplayString()));
                    }

                    var route = attribute.ConstructorArguments[0].Value?.ToString() ?? "/unknown";

                    var policyName = attribute.ConstructorArguments.Length > 1
                        ? attribute.ConstructorArguments[1].Value?.ToString() ?? "Default"
                        : "Default";

                    var cqsType = "Unknown";
                    if (attribute.ConstructorArguments.Length > 2)
                    {
                        var requestTypeConstant = attribute.ConstructorArguments[2];
                        if (requestTypeConstant.Value is int val &&
                            requestTypeConstant.Type is INamedTypeSymbol enumType)
                        {
                            var member = enumType.GetMembers().OfType<IFieldSymbol>()
                                .FirstOrDefault(f => f.HasConstantValue && (int)f.ConstantValue == val);
                            cqsType = member?.Name ?? "Unknown";
                        }
                    }

                    var hasIdentityId = symbol.GetMembers().Any(m => m.Name == "IdentityId");
                    var type = symbol.IsRecord ? "Record" : "Class";

                    return new RequestData(
                        route,
                        policyName,
                        hasIdentityId,
                        cqsType,
                        symbol.Name,
                        fullName,
                        returnValueFullName.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        type,
                        fieldsBuilder.ToImmutable()
                    );
                })
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }
}