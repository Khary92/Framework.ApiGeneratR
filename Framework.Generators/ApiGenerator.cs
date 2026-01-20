using System.Collections.Immutable;
using System.Text;
using Framework.Generators.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Framework.Generators;

[Generator(LanguageNames.CSharp)]
public class ApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax c &&
                                            c.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null);

        var collectedHandlers = handlerDeclarations.Collect();

        context.RegisterSourceOutput(collectedHandlers,
            static (spc, handlers) => Execute(spc, handlers));
    }

    private static INamedTypeSymbol? GetSemanticTarget(GeneratorSyntaxContext ctx)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null) return null;

        var hasApiDefinitionAttr = classSymbol.GetAttributes()
            .Any(ad => ad.AttributeClass?.Name == "ApiDefinition" ||
                       ad.AttributeClass?.Name == "ApiDefinitionAttribute");

        return hasApiDefinitionAttr ? classSymbol : null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> handlers)
    {
        CreateExtensionMethod(context, handlers);
    }

    private static void CreateExtensionMethod(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> handlers)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["Microsoft.AspNetCore.Builder"]);
        scb.SetNamespace("ApiGeneratR.Generated");

        scb.StartScope("public static class ApiExtensions");
        scb.StartScope("public static void AddApiEndpoints(this WebApplication app)");

        if (!handlers.IsDefaultOrEmpty)
        {
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var attribute = handler.GetAttributes()
                    .First(a => a.AttributeClass?.Name == "ApiDefinition");

                var route = attribute.ConstructorArguments[0].Value?.ToString();
                var handlerType = handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                scb.StartScope(
                    $"app.MapPost(\"{route}\", async ({handlerType} request, global::Framework.Contract.Mediator.IMediator mediator) =>");
                scb.AddLine("return await mediator.HandleAsync(request);");
                scb.EndScope(");");
                scb.AddLine("");
            }
        }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("ApiExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}