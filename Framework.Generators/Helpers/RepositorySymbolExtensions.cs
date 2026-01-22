using System.Collections.Immutable;
using Framework.Generators.Generators.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.Generators.Helpers;

public static class RepositorySymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<RepositorySourceData>> GetRepositorySourceData(
        this IncrementalGeneratorInitializationContext context, string attributeName)
    {
        var classSymbols = context.GetAttributeAnnotatedClassSymbols(attributeName);
        var recordSymbols = context.GetAttributeAnnotatedRecordSymbols(attributeName);

        var combined = classSymbols.Combine(recordSymbols);

        return combined.Select((pair, _) =>
        {
            var (classes, records) = pair;
            var all = classes.AddRange(records);

            var builder = ImmutableArray.CreateBuilder<RepositorySourceData>(all.Length);
            foreach (var symbol in all)
            {
                if (symbol == null) continue;

                var attr = symbol.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == "Framework.Contract.Attributes.DomainEntityAttribute"
                    || a.AttributeClass?.ToDisplayString() == "Framework.Contract.Attributes.DomainEntity");

                if (attr == null || attr.ConstructorArguments[0].Value == null) continue;

                builder.Add(new RepositorySourceData(symbol.Name,
                    symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    symbol.ContainingNamespace.ToDisplayString(), attr.ConstructorArguments[0].Value!.ToString()));
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