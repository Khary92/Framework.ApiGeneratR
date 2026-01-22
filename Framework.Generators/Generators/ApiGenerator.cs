using System.Collections.Immutable;
using System.Text;
using Framework.Generators.Builder;
using Framework.Generators.Generators.Mapper;
using Framework.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Framework.Generators.Generators;

[Generator(LanguageNames.CSharp)]
public class ApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var apiSourceData = context.GetApiSourceData("ApiDefinition");

        context.RegisterSourceOutput(apiSourceData,
            static (spc, apiDefinitions) => ExecuteCodeGeneration(spc, apiDefinitions));

        context.RegisterSourceOutput(apiSourceData,
            static (spc, apiDefinitions) => ExecuteDocuGeneration(spc, apiDefinitions));
    }

    private static void ExecuteDocuGeneration(SourceProductionContext context,
        ImmutableArray<ApiSourceData> apiDefinitions)
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
                rows.Add([
                    $"`{handler.HttpMethod}`", $"{handler.RequiresAuth}", $"`{handler.Route}`", handler.ShortName,
                    handler.Type
                ]);
            }

            mdb.AddTable(["Method", "Requires Auth", "Route", "Command/Record", "Type"], rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Request Definitions", 2);

            foreach (var definition in apiDefinitions)
            {
                if (definition == null) continue;

                mdb.AddHeader(definition.ShortName, 3);
                mdb.AddParagraph($"Full Type: `{definition.FullName}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {definition.ShortName}");

                foreach (var member in definition.Members)
                {
                    mdb.AddLine(member);
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
        ImmutableArray<ApiSourceData> apiDefinitions)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["Microsoft.AspNetCore.Builder"]);
        scb.SetNamespace("Framework.Generated");

        scb.StartScope("public static class ApiExtensions");
        scb.StartScope("public static void AddApiEndpoints(this WebApplication app)");

        if (!apiDefinitions.IsDefaultOrEmpty)
        {
            foreach (var definition in apiDefinitions)
            {
                if (definition == null) continue;
                scb.StartScope(
                    $"app.MapPost(\"{definition.Route}\", async ({definition.RequestType} request, global::Framework.Contract.Mediator.IMediator mediator) =>");
                scb.AddLine("return await mediator.HandleAsync(request);");
                scb.EndScope(");");
            }
        }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("ApiExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}