using System;
using System.Collections.Immutable;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

[Generator(LanguageNames.CSharp)]
public class ClientApiInjectorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var apiSourceData = context.GetConsumerApiSourceData();
        var combined = apiSourceData.Combine(assemblyName).Combine(context.GetGlobalOptions());

        context.RegisterSourceOutput(combined,
            static (spc, source) =>
            {
                try
                {
                    Execute(spc, source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "Api Injection Generator Error",
                            "Error generating api injection code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<ApiConsumerData> consumerData,
        string? nameSpace, GlobalOptions options)
    {
        if (nameSpace == null || !options.IsClientProject(nameSpace)) return;

        ExecuteExtensionMethodGeneration(spc, consumerData);
    }

    private static void ExecuteExtensionMethodGeneration(SourceProductionContext context,
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

            context.AddSource($"{consumer.ConsumerClassName}.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
}