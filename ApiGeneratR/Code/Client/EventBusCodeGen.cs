using System;
using ApiGeneratR.Code.Builder;

namespace ApiGeneratR.Code.Client;

public static class EventBusCodeGen
{
    public static SourceCodeFile Create(Language targetLanguage, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return CreateCsharpEventBusWithInterfaces(projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return CreateCsharpEventBusWithInterfaces(TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile CreateCsharpEventBusWithInterfaces(string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace(projectNamespace);
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

        return new SourceCodeFile("EventBus.g.cs", scb.ToString());
    }
}