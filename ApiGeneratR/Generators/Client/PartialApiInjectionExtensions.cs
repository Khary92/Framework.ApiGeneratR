using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Generators.Client;

public static class PartialApiInjectionExtensions
{
    public static void CreatePartialClasses(this SourceProductionContext ctx,
        ImmutableArray<ApiConsumerData> consumerData, GlobalOptions options)
    {
        foreach (var consumer in consumerData)
        {
            if (consumer == null) continue;

            var scb = new SourceCodeBuilder();
            scb.SetUsings([
                "System.Threading.Tasks",
                "System.Collections.Generic",
                "System.Threading"
            ]);
            scb.SetNamespace(consumer.ConsumerNamespace);

            scb.StartScope($"public partial class {consumer.ConsumerClassName} : IDisposable");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.IApiContainer _container; ");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.IWebSocketService WebSocket;");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.ICommandSender Commands;");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.IQuerySender Queries;");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.IEventPublisher EventPublisher;");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.IEventSubscriber EventSubscriber;");
            scb.AddLine();
            scb.StartScope(
                $"public {consumer.ConsumerClassName}(global::{options.DefinitionsProject}.Generated.IApiContainer container)");
            scb.AddLine("_container = container;");
            scb.AddLine("WebSocket = container.WebSocket;");
            scb.AddLine("Commands = container.Commands;");
            scb.AddLine("Queries = container.Queries;");
            scb.AddLine("EventPublisher = container.EventPublisher;");
            scb.AddLine("EventSubscriber = container.EventSubscriber;");
            scb.AddLine("Initialize();");
            scb.EndScope();
            scb.AddLine();
            scb.StartScope("private void SetToken(string token)");
            scb.AddLine("_container.SetToken(token);");
            scb.EndScope();
            scb.AddLine();
            scb.StartScope("private void Initialize()");

            foreach (var registeredEvent in consumer.TypeNames)
            {
                if (registeredEvent == null) continue;

                scb.AddLine(
                    $"_ = EventSubscriber.Subscribe<{registeredEvent.EventLongName}>(Handle{registeredEvent.EventShortName}Async);");
            }

            scb.EndScope();
            scb.AddLine();
            scb.StartScope("public void Dispose()");
            foreach (var registeredEvent in consumer.TypeNames)
            {
                if (registeredEvent == null) continue;

                scb.AddLine(
                    $"EventSubscriber.Unsubscribe<{registeredEvent.EventLongName}>(Handle{registeredEvent.EventShortName}Async);");
            }
            scb.EndScope();
            scb.EndScope();

            ctx.AddSource($"{consumer.ConsumerClassName}.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
}