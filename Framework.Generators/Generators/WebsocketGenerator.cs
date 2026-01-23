using System.Collections.Immutable;
using System.Text;
using Framework.Generators.Builder;
using Framework.Generators.Generators.Mapper;
using Framework.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Framework.Generators.Generators;

[Generator]
public class WebsocketGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var socketEvents = context.GetWebsocketSourceData("WebsocketEvent");

        context.RegisterSourceOutput(socketEvents,
            static (spc, events) =>
            {
                try
                {
                    ExecuteDocuGeneration(spc, events);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("WEBSOCKGEN001", "Generator crashed", "{0}", "WebsocketGenerator",
                            DiagnosticSeverity.Error, true), Location.None, ex.Message));
                }
            });

        context.RegisterSourceOutput(socketEvents,
            static (spc, events) =>
            {
                try
                {
                    ExecuteMapperGeneration(spc, events);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("WEBSOCKGEN002", "Generator crashed", "{0}", "WebsocketGenerator",
                            DiagnosticSeverity.Error, true), Location.None, ex.Message));
                }
            });
    }

    private static void ExecuteMapperGeneration(SourceProductionContext spc,
        ImmutableArray<WebsocketEventSourceData> events)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["System.Text.Json", "Framework.Contract.Attributes"]);

        scb.SetNamespace("Framework.Generated");
        scb.StartScope("public static class WebsocketExtensions");

        if (!events.IsDefaultOrEmpty)
        {
            foreach (var @event in events)
            {
                if (@event == null) continue;
                scb.StartScope($"public static string ToSocketMessage(this {@event.FullTypeName} @event)");
                scb.AddLine("var json = JsonSerializer.Serialize(@event);");
                scb.AddLine($"return $\"{@event.EventType},\" + json;");
                scb.EndScope();
            }
        }

        scb.EndScope();

        spc.AddSource("WebsocketExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteDocuGeneration(SourceProductionContext spc,
        ImmutableArray<WebsocketEventSourceData> events)
    {
        var mdb = new MarkdownBuilder();
        mdb.AddHeader("Websocket Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the websocket events. Total events: {events.Length}");

        if (events.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            mdb.AddHeader("Event Definitions", 2);

            foreach (var definition in events)
            {
                if (definition == null) continue;

                mdb.AddHeader(definition.TypeName, 3);
                mdb.AddParagraph($"Full Type: `{definition.FullTypeName}` ");

                if (!string.IsNullOrEmpty(definition.Description))
                {
                    mdb.AddParagraph("Description: " + definition.Description);
                }

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {definition.TypeName}");

                foreach (var member in definition.Properties)
                {
                    mdb.AddLine(member.Type + " " + member.Name);
                }

                mdb.EndCodeBlock();
            }
        }

        SourceCodeBuilder scb = new();
        scb.SetNamespace("Framework.Generated");
        scb.StartScope("public class WebsocketDocumentation : global::Framework.Contract.Documentation.IDocumentation");
        scb.AddLine();

        scb.AddLine("public string FileName => \"WebsocketDocumentation.md\";");
        scb.AddLine("public string Markdown => \"\"\"");

        var lines = mdb.ToString().Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            scb.AddLine(line);
        }

        scb.AddLine("\"\"\";");
        scb.EndScope();

        spc.AddSource("WebsocketDocumentation.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}