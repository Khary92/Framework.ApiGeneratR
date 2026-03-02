using System;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

[Generator(LanguageNames.CSharp)]
public class EventBusGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = assemblyName.Combine(context.GetGlobalOptions());
        
        context.RegisterSourceOutput(combined,
            static (spc, combined) =>
            {
                try
                {
                    Execute(spc, combined.Left, combined.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "ServerApiGenerator Error",
                            "Error generating event bus code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }
    
    private static void Execute(SourceProductionContext context, string? projectNamespace, GlobalOptions options)
    {
        if (projectNamespace != options.DefinitionsProject) return;
        
        var scb = new SourceCodeBuilder();
        
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public interface IEventPublisher");
        scb.AddLine("Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public interface IEventSubscriber");
        scb.AddLine("Task Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;");
        scb.AddLine("void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public class EventService : IEventSubscriber, IEventPublisher");
        scb.AddLine("private readonly Dictionary<Type, List<Delegate>> _handlers = new();");
        scb.AddLine("private readonly Lock _lock = new();");
        scb.StartScope("public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class");
        scb.AddLine("List<Delegate> handlersCopy;");
        scb.AddLine();
        scb.StartScope("lock (_lock)");
        scb.AddLine("if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;");
        scb.AddLine("handlersCopy = new List<Delegate>(handlers);");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("var tasks = handlersCopy.Cast<Func<TEvent, Task>>().Select(h => h(@event));");
        scb.AddLine("await Task.WhenAll(tasks);");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public Task Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class");
        scb.StartScope("lock (_lock)");
        scb.StartScope("if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))");
        scb.AddLine("handlers = [];");
        scb.AddLine("_handlers[typeof(TEvent)] = handlers;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("handlers.Add(handler);");
        scb.EndScope();
        scb.AddLine("return Task.CompletedTask;");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class");
        scb.StartScope("lock (_lock)");
        scb.StartScope("if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;");
        scb.AddLine();
        scb.AddLine("handlers.Remove(handler);");
        scb.AddLine();
        scb.AddLine("if (handlers.Count == 0) _handlers.Remove(typeof(TEvent));");
        scb.EndScope();
        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        context.AddSource("EventBus.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8)); 
    }
}