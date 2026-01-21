using System.Collections.Immutable;
using System.Text;
using Framework.Generators.Builder;
using Framework.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Framework.Generators.Generators;

[Generator(LanguageNames.CSharp)]
public class ApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var left = context.GetAttributeAnnotatedClassSymbols("ApiDefinition");
        var right = context.GetAttributeAnnotatedRecordSymbols("ApiDefinition");
        
        var combined = left.Combine(right)
            .Select((pair, _) => pair.Left.AddRange(pair.Right));
        
        context.RegisterSourceOutput(combined,
            static (spc, handlers) => Execute(spc, handlers));
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> handlers)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["Microsoft.AspNetCore.Builder"]);
        scb.SetNamespace("Framework.Generated");

        scb.StartScope("public static class ApiExtensions");
        scb.StartScope("public static void AddApiEndpoints(this WebApplication app)");

        if (!handlers.IsDefaultOrEmpty)
        {
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var attribute = handler.GetAttributes()
                    .First(a => a.AttributeClass?.Name.StartsWith("ApiDefinition") == true);

                var route = attribute.ConstructorArguments[0].Value?.ToString();
                var handlerType = handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                scb.StartScope(
                    $"app.MapPost(\"{route}\", async ({handlerType} request, global::Framework.Contract.Mediator.IMediator mediator) =>");
                scb.AddLine("return await mediator.HandleAsync(request);");
                scb.EndScope(");");
            }
        }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("ApiExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}