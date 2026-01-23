using System.Collections.Immutable;
using Framework.Generators.Generators.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.Generators.Helpers;

public static class WebsocketSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<WebsocketEventSourceData>> GetWebsocketSourceData(
        this IncrementalGeneratorInitializationContext context,
        string attributeName)
    {
        var classSymbols = context.GetAttributeAnnotatedClassSymbols(attributeName);
        var recordSymbols = context.GetAttributeAnnotatedRecordSymbols(attributeName);

        var combined = classSymbols.Combine(recordSymbols);

        var shortName = attributeName.EndsWith("Attribute")
            ? attributeName.Substring(0, attributeName.Length - 9)
            : attributeName;

        return combined.Select((pair, _) =>
        {
            var (classes, records) = pair;
            var all = classes.AddRange(records);

            var builder = ImmutableArray.CreateBuilder<WebsocketEventSourceData>(all.Length);

            foreach (var symbol in all)
            {
                if (symbol is null) continue;

                var arrayBuilder = ImmutableArray.CreateBuilder<FieldData>();
                foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility != Accessibility.Public) continue;
                    if (member.IsStatic) continue;
                    if (member.IsIndexer) continue;


                    arrayBuilder.Add(new FieldData(member.Name, member.Type.ToDisplayString()));
                }

                var attribute = symbol.GetAttributes()
                    .FirstOrDefault(a =>
                    {
                        var name = a.AttributeClass?.Name;
                        return name == shortName || name == $"{shortName}Attribute";
                    });

                if (attribute?.AttributeClass is null)
                    continue;

                var @namespace = attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() ?? "UnknownNamespace";

                var eventType = attribute.ConstructorArguments.Length > 0
                    ? attribute.ConstructorArguments[0].Value?.ToString() ?? "EventTypeError"
                    : "EventTypeError";

                var description = attribute.NamedArguments
                    .FirstOrDefault(a => a.Key == "Description")
                    .Value.Value?.ToString() ?? string.Empty;

                builder.Add(new WebsocketEventSourceData(@namespace, symbol.Name,
                    symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    eventType, description, arrayBuilder.ToImmutable()));
            }

            return builder.ToImmutable();
        });
    }


    private static IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> GetAttributeAnnotatedClassSymbols(
        this IncrementalGeneratorInitializationContext context, string attributeName)
    {
        var shortName = attributeName.EndsWith("Attribute")
            ? attributeName.Substring(0, attributeName.Length - 9)
            : attributeName;

        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: (ctx, _) => GetSemanticClassTarget(ctx, shortName))
            .Where(static m => m is not null);

        return handlerDeclarations.Collect();
    }

    private static INamedTypeSymbol? GetSemanticClassTarget(GeneratorSyntaxContext ctx, string shortName)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null) return null;

        var hasAttribute = classSymbol.GetAttributes()
            .Any(ad =>
            {
                var name = ad.AttributeClass?.Name;
                return name == shortName || name == $"{shortName}Attribute";
            });

        return hasAttribute ? classSymbol : null;
    }

    private static IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> GetAttributeAnnotatedRecordSymbols(
        this IncrementalGeneratorInitializationContext context, string attributeName)
    {
        var shortName = attributeName.EndsWith("Attribute")
            ? attributeName.Substring(0, attributeName.Length - 9)
            : attributeName;

        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is RecordDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: (ctx, _) => GetSemanticRecordTarget(ctx, shortName))
            .Where(static m => m is not null);

        return handlerDeclarations.Collect();
    }

    private static INamedTypeSymbol? GetSemanticRecordTarget(GeneratorSyntaxContext ctx, string shortName)
    {
        var recordDecl = (RecordDeclarationSyntax)ctx.Node;
        var recordSymbol = ctx.SemanticModel.GetDeclaredSymbol(recordDecl) as INamedTypeSymbol;

        if (recordSymbol == null) return null;

        var hasAttribute = recordSymbol.GetAttributes()
            .Any(ad =>
            {
                var name = ad.AttributeClass?.Name;
                return name == shortName || name == $"{shortName}Attribute";
            });

        return hasAttribute ? recordSymbol : null;
    }
}