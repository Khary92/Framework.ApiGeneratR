using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Code.Api;

public static class EventSerializationCodeGen
{
    public static SourceCodeFile Create(string projectNamespace)
    {
        return ExecuteCSharpEnvelopeGeneration(projectNamespace);
    }
    
    private static SourceCodeFile ExecuteCSharpEnvelopeGeneration(string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public class EventEnvelope(string type, string payload, DateTimeOffset timestamp)");
        scb.AddLine("public string Type { get; set; } = type;");
        scb.AddLine("public string Payload { get; set; } = payload;");
        scb.AddLine("public DateTimeOffset Timestamp { get; set; } = timestamp;");
        scb.EndScope();

        return new SourceCodeFile("EventEnvelope.g.cs", scb.ToString());
    }
}