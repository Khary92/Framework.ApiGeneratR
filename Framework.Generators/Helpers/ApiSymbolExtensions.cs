using System.Collections.Immutable;
using Framework.Generators.Generators.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.Generators.Helpers;

public static class ApiSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<ApiSourceData>> GetApiSourceData(
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

            var builder = ImmutableArray.CreateBuilder<ApiSourceData>(all.Length);

            foreach (var symbol in all)
            {
                if (symbol is null) continue;

                var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var apiRequestTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var arrayBuilder = ImmutableArray.CreateBuilder<string>();
                foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility != Accessibility.Public) continue;
                    if (member.IsStatic) continue;
                    if (member.IsIndexer) continue;

                    arrayBuilder.Add($"public {member.Type.ToDisplayString()} {member.Name} {{ get; }}");
                }

                var attribute = symbol.GetAttributes()
                    .FirstOrDefault(a =>
                    {
                        var name = a.AttributeClass?.Name;
                        return name == shortName || name == $"{shortName}Attribute";
                    });

                if (attribute?.AttributeClass is null)
                    continue;

                var route =
                    attribute.ConstructorArguments.Length > 0
                        ? attribute.ConstructorArguments[0].Value?.ToString() ?? "/unknown"
                        : "/unknown";

                var requiresAuth = attribute.NamedArguments
                    .FirstOrDefault(a => a.Key == "RequiresAuth")
                    .Value.Value as bool? ?? false;

                var httpMethod = attribute.NamedArguments
                    .FirstOrDefault(a => a.Key == "Method")
                    .Value.Value?.ToString() ?? "POST";

                var name = symbol.Name;
                var type = symbol.IsRecord ? "Record" : "Class";

                builder.Add(new ApiSourceData(route, requiresAuth, httpMethod, apiRequestTypeName, name, fullName,
                    arrayBuilder.ToImmutable(), type));
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