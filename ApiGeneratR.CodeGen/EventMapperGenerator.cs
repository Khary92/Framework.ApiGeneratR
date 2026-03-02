using System;
using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

[Generator]
public class EventMapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var socketEvents = context.GetEventSourceData("ApiGeneratR.Definitions");
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = socketEvents.Combine(assemblyName);
        context.RegisterSourceOutput(combined,
            static (spc, source) =>
            {
                try
                {
                    ExecuteWebsocketsExtensions(spc, source.Left, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("WEBSOCKGEN001", "Generator crashed", "{0}", "EventMapperGenerator",
                            DiagnosticSeverity.Error, true), Location.None, ex.Message));
                }
            });
    }

    private static void ExecuteWebsocketsExtensions(SourceProductionContext spc,
        ImmutableArray<EventSourceData> events, string? projectNamespace)
    {
        if (events.IsDefaultOrEmpty) return;

        if (projectNamespace is not "ApiGeneratR.Definitions") return;

        foreach (var @event in events)
        {
            if (@event == null) continue;

            var scb = new SourceCodeBuilder();

            scb.SetUsings(["System.Text.Json"]);

            scb.SetNamespace($"{projectNamespace}.Generated");

            scb.StartScope($"public static class {@event.TypeName}WebsocketExtensions");

            scb.StartScope(
                $"public static global::ApiGeneratR.Definitions.Generated.EventEnvelope ToWebsocketMessage(this {@event.FullTypeName} @event)");
            scb.AddLine(
                $"return new(\"{@event.EventType}\", JsonSerializer.Serialize(@event), DateTime.UtcNow);");
            scb.EndScope();
            scb.EndScope();

            spc.AddSource($"{@event.TypeName}WebsocketsExtensions.g.cs",
                SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
}