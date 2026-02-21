using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Shared.Contract.Generator.Builder;
using Shared.Contract.Generator.Helpers;
using Shared.Contract.Generator.Mapper;

namespace Shared.Contract.Generator;

[Generator]
public class EventMapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var socketEvents = context.GetEventSourceData();
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

        if (projectNamespace is not "Api.Definitions") return;

        foreach (var @event in events)
        {
            if (@event == null) continue;

            var scb = new SourceCodeBuilder();

            scb.SetUsings(["System.Text.Json"]);

            scb.SetNamespace($"{@event.Namespace}.Generated");

            scb.StartScope($"public static class {@event.TypeName}WebsocketExtensions");

            scb.StartScope(
                $"public static global::Shared.Contracts.EventBus.EventEnvelope ToWebsocketMessage(this {@event.FullTypeName} @event)");
            scb.AddLine(
                $"return new(\"{@event.EventType}\", JsonSerializer.Serialize(@event), DateTime.UtcNow);");
            scb.EndScope();
            scb.EndScope();

            spc.AddSource($"{@event.TypeName}WebsocketsExtensions.g.cs",
                SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
}