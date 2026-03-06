using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen.Generators.Client;

public static class DocumentationExtensions
{
    public static void CreateStaticServices(this SourceProductionContext ctx, string projectNamespace,
        ImmutableArray<EventSourceData> events, ImmutableArray<RequestData> requests, GlobalOptions options)
    {
        SourceCodeBuilder scb = new();
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public static class GeneratRStaticServices");
        scb.AddLine();
        
        scb.AddLine("private static string Markdown => \"\"\"");
        var lines = GetMarkdownText(events, requests).Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines) scb.AddLine(line);
        scb.AddLine("\"\"\";");
        
        scb.AddLine();
        scb.StartScope("public static void PrintDocumentationToPath(string path)");
        scb.AddLine("File.WriteAllText(path, Markdown);");
        scb.EndScope();
        scb.EndScope();
        
        ctx.AddSource("StaticServices.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static string GetMarkdownText(ImmutableArray<EventSourceData> events, ImmutableArray<RequestData> requests)
    {
        var mdb = new MarkdownBuilder();
        mdb.AddHeader("API Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the available endpoints. Total endpoints: {requests.Length}");

        if (requests.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            mdb.AddHeader("Endpoints Overview", 2);

            var rows = new List<List<string>>();
            foreach (var handler in requests)
                rows.Add([
                    $"`{handler.HttpMethod}`", $"{handler.RequiresAuth}", $"`{handler.Route}`",
                    handler.RequestShortName,
                    handler.DataStructureType
                ]);

            mdb.AddTable(new List<string> { "Method", "Requires Auth", "Route", "Command/Record", "Type" }, rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Request Definitions", 2);

            foreach (var request in requests)
            {
                if (request == null) continue;

                mdb.AddHeader(request.RequestShortName, 3);
                mdb.AddParagraph($"Full Type: `{request.RequestFullName}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {request.RequestShortName}");

                foreach (var member in request.Members) mdb.AddLine(member);

                mdb.EndCodeBlock();
            }
        }

        mdb.AddHorizontalRule();

        mdb.AddHeader("Event Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the distributed events. Total events: {events.Length}");

        if (events.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            foreach (var @event in events)
            {
                if (@event == null) continue;

                mdb.AddHeader(@event.TypeName, 3);
                mdb.AddParagraph($"Full Type: `{@event.FullTypeName}` ");
                mdb.AddParagraph($"Deserialization reference: `{@event.EventType}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {@event.TypeName}");

                foreach (var member in @event.Properties)
                {
                    mdb.AddLine($"public {member.Type} {member.Name} " + "{ get; }");
                }

                mdb.EndCodeBlock();
            }
        }

        return mdb.ToString();
    }
}