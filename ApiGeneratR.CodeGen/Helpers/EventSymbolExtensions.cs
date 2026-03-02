using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiGeneratR.CodeGen.Helpers;

public static class EventSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<EventSourceData>> GetEventSourceData(
        this IncrementalGeneratorInitializationContext context, string nameSpace)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{nameSpace}.Generated.EventAttribute",
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

                    return new EventSourceData(
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