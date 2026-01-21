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
            static (spc, apiDefinitions) => ExecuteCodeGeneration(spc, apiDefinitions));

        context.RegisterSourceOutput(combined,
            static (spc, apiDefinitions) => ExecuteDocuGeneration(spc, apiDefinitions));
    }

    private static void ExecuteDocuGeneration(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> apiDefinitions)
    {
        var mdb = new MarkdownBuilder();
        mdb.AddHeader("API Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the available endpoints. Total endpoints: {apiDefinitions.Length}");

        if (apiDefinitions.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            mdb.AddHeader("Endpoints Overview", 2);

            var rows = new List<List<string>>();
            foreach (var handler in apiDefinitions)
            {
                if (handler == null) continue;

                var attribute = handler.GetAttributes()
                    .First(a => a.AttributeClass?.Name.StartsWith("ApiDefinition") == true);

                var route = attribute.ConstructorArguments[0].Value?.ToString() ?? "/unknown";
                var requiresAuth = attribute.NamedArguments.Any(a => a.Key == "RequiresAuth");
                var httpMethod =
                    attribute.NamedArguments.FirstOrDefault(a => a.Key == "Method").Value.Value?.ToString() ?? "POST";
                var name = handler.Name;
                var type = handler.IsRecord ? "Record" : "Class";

                rows.Add([$"`{httpMethod}`", $"{requiresAuth}", $"`{route}`", name, type]);
            }

            mdb.AddTable(["Method", "Requires Auth", "Route", "Command/Record", "Type"], rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Request Definitions", 2);

            foreach (var handler in apiDefinitions)
            {
                if (handler == null) continue;

                mdb.AddHeader(handler.Name, 3);
                mdb.AddParagraph($"Full Type: `{handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {handler.Name}");

                foreach (var member in handler.GetMembers().OfType<IPropertySymbol>())
                {
                    mdb.AddLine($"public {member.Type.Name} {member.Name} {{ get; }}");
                }

                mdb.EndCodeBlock();
            }
        }

        SourceCodeBuilder scb = new();
        scb.SetNamespace("Framework.Generated");
        scb.StartScope("public class ApiDocumentation : global::Framework.Contract.Documentation.IDocumentation");
        scb.AddLine();

        scb.AddLine("public string FileName => \"ApiDocumentation.md\";");
        scb.AddLine("public string Markdown => \"\"\"");
        
        var lines = mdb.ToString().Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            scb.AddLine(line);
        }

        scb.AddLine("\"\"\";"); 
        scb.EndScope();

        context.AddSource("ApiDocumentation.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteCodeGeneration(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> apiDefinitions)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["Microsoft.AspNetCore.Builder"]);
        scb.SetNamespace("Framework.Generated");

        scb.StartScope("public static class ApiExtensions");
        scb.StartScope("public static void AddApiEndpoints(this WebApplication app)");

        if (!apiDefinitions.IsDefaultOrEmpty)
        {
            foreach (var handler in apiDefinitions)
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