using System.Collections.Generic;
using System.Collections.Immutable;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Client;

public class PartialEventReceiverCodeGen
{
    public static List<SourceCodeFile> Create(ImmutableArray<ApiConsumerData> consumerData, GlobalOptions options)
    {
        return CreateCSharpPartialClasses(consumerData, options);
    }

    private static List<SourceCodeFile> CreateCSharpPartialClasses(ImmutableArray<ApiConsumerData> consumerData,
        GlobalOptions options)
    {
        var result = new List<SourceCodeFile>();

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
            scb.AddLine(
                $"private readonly global::{options.DefinitionsProject}.Generated.IEventReceiver EventReceiver;");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.ICommandSender Commands;");
            scb.AddLine($"private readonly global::{options.DefinitionsProject}.Generated.IQuerySender Queries;");
            scb.AddLine(
                $"private readonly global::{options.DefinitionsProject}.Generated.IEventPublisher EventPublisher;");
            scb.AddLine(
                $"private readonly global::{options.DefinitionsProject}.Generated.IEventSubscriber EventSubscriber;");
            scb.AddLine();
            scb.StartScope(
                $"public {consumer.ConsumerClassName}(global::{options.DefinitionsProject}.Generated.IApiContainer container)");
            scb.AddLine("_container = container;");
            scb.AddLine("EventReceiver = container.EventReceiver;");
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

            result.Add(new SourceCodeFile($"{consumer.ConsumerClassName}.g.cs", scb.ToString()));
        }

        return result;
    }
}