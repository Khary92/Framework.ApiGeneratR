using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.Helpers;

public static class EventAttributeRetriever
{
    public static IncrementalValueProvider<ImmutableArray<EventData>> GetEventSourceData(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                "ApiGeneratR.Attributes.EventAttribute",
                (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var attribute = ctx.Attributes.FirstOrDefault();

                    if (attribute is null) return null;

                    var fieldsBuilder = ImmutableArray.CreateBuilder<FieldData>();
                    foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (member.DeclaredAccessibility != Accessibility.Public) continue;
                        if (member.IsStatic || member.IsIndexer) continue;

                        fieldsBuilder.Add(new FieldData(member.Name, member.Type.ToDisplayString()));
                    }

                    var @namespace = symbol.ContainingNamespace?.ToDisplayString() ?? "UnknownNamespace";

                    var eventType = attribute.ConstructorArguments.Length > 0
                        ? attribute.ConstructorArguments[0].Value?.ToString() ?? "EventTypeError"
                        : "EventTypeError";

                    return new EventData(
                        @namespace,
                        symbol.Name,
                        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        eventType,
                        fieldsBuilder.ToImmutable());
                })
            .Where(x => x is not null)
            .Select((x, _) => x!)
            .Collect();
    }
}