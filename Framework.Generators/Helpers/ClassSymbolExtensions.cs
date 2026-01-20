using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.Generators.Helpers;

public static class ClassSymbolExtensions
{
    public static IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> GetAttributeAnnotatedClassSymbols(
        this IncrementalGeneratorInitializationContext context, string attributeName)
    {
        var shortName = attributeName.EndsWith("Attribute") 
            ? attributeName.Substring(0, attributeName.Length - 9) 
            : attributeName;

        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: (ctx, _) => GetSemanticTarget(ctx, shortName))
            .Where(static m => m is not null)!;
        
        return handlerDeclarations.Collect();
    }

    private static INamedTypeSymbol? GetSemanticTarget(GeneratorSyntaxContext ctx, string shortName)
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
}