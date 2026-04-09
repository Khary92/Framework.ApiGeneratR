using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Code.Server;

public static class WebSocketDependencyInjectionCodeGen
{
    public static List<SourceCodeFile> Create(ImmutableArray<EventData> events, string? projectNamespace,
        GlobalOptions options)
    {
        var result = new List<SourceCodeFile>();

        foreach (var @event in events)
        {
            if (@event == null) continue;

            var scb = new SourceCodeBuilder();

            scb.SetUsings(["System.Text.Json"]);

            scb.SetNamespace($"{projectNamespace}.Generated");

            scb.StartScope($"public static class {@event.TypeName}WebsocketExtensions");

            scb.StartScope(
                $"public static global::{options.DefinitionsProject}.Generated.EventEnvelope ToWebsocketMessage(this {@event.FullTypeName} @event)");
            scb.AddLine(
                $"return new(\"{@event.EventType}\", JsonSerializer.Serialize(@event), DateTimeOffset.Now);");
            scb.EndScope();
            scb.EndScope();

            result.Add(new SourceCodeFile($"{@event.TypeName}WebsocketsExtensions.g.cs", scb.ToString()));
        }

        return result;
    }
}