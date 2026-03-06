using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen.Generators.Client;

public static class PartialApiInjectionExtensions
{
    public static void CreatePartialClasses(this SourceProductionContext ctx,
        ImmutableArray<ApiConsumerData> consumerData)
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
            scb.AddLine("private readonly List<IDisposable> _disposables = [];");
            scb.AddLine("private readonly global::ApiGeneratR.Definitions.Generated.IApiContainer _container; ");
            scb.AddLine("private readonly global::ApiGeneratR.Definitions.Generated.IWebSocketService WebSocket;");
            scb.AddLine("private readonly global::ApiGeneratR.Definitions.Generated.ICommandSender Commands;");
            scb.AddLine("private readonly global::ApiGeneratR.Definitions.Generated.IQuerySender Queries;");
            scb.AddLine("private readonly global::ApiGeneratR.Definitions.Generated.IEventPublisher EventPublisher;");
            scb.AddLine("private readonly global::ApiGeneratR.Definitions.Generated.IEventSubscriber EventSubscriber;");
            scb.AddLine();
            scb.StartScope(
                $"public {consumer.ConsumerClassName}(global::ApiGeneratR.Definitions.Generated.IApiContainer container)");
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

            foreach (var registeredEvent in consumer.GlobalEventTypesNameSpaces)
            {
                if (registeredEvent == null) continue;

                scb.AddLine(
                    $"_disposables.Add(EventSubscriber.Subscribe<{registeredEvent}>((@event) => HandleEventAsync(@event)));");
            }

            scb.EndScope();
            scb.AddLine();
            scb.StartScope("public void Dispose()");
            scb.AddLine("foreach (var disposable in _disposables) disposable.Dispose();");
            scb.EndScope();
            scb.EndScope();

            ctx.AddSource($"{consumer.ConsumerClassName}.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
}