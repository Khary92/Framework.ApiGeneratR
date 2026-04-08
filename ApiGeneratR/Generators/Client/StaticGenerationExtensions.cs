using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Generators.Client;

public static class StaticGenerationExtensions
{
    public static void CreateApiDocumentation(this SourceProductionContext ctx, string projectNamespace,
        ImmutableArray<EventData> events, ImmutableArray<RequestData> requests)
    {
        SourceCodeBuilder scb = new();
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public static class GeneratedDocumentation");
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

        ctx.AddSource("GeneratedDocumentation.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
    
    public static void CreateTranspilerStatic(this SourceProductionContext ctx, TranspilerBuilder transpilerBuilder)
    {
        ctx.AddSource("GeneratedTranspiler.g.cs", SourceText.From(transpilerBuilder.GetStaticSourceFile(), Encoding.UTF8));
    }

    private static string GetMarkdownText(ImmutableArray<EventData> events, ImmutableArray<RequestData> requests)
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
            foreach (var request in requests)
                rows.Add([
                    request.AuthPolicy,
                    request.RequestShortName,
                    request.DataStructureType
                ]);

            mdb.AddTable(new List<string> { "Auth Policy", "Route", "Command/Record", "Type" }, rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Request Definitions", 2);

            foreach (var request in requests)
            {
                if (request == null) continue;

                mdb.AddHeader(request.RequestShortName, 3);
                mdb.AddParagraph($"Full Type: `{request.RequestFullName}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {request.RequestShortName}");

                foreach (var member in request.Properties)
                    mdb.AddLine("public " + member.Type + " " + member.Name + " { get; }");

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