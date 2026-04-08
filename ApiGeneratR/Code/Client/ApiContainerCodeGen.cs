using System;
using ApiGeneratR.Builder;
using ApiGeneratR.Code.Builder;

namespace ApiGeneratR.Code.Client;

public static class ApiContainerCodeGen
{
    public static SourceCodeFile Create(Language targetLanguage, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return CreateCSharpApiContainer(projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return CreateCSharpApiContainer(TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile CreateCSharpApiContainer(string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Threading"
        ]);

        scb.SetNamespace(projectNamespace);

        scb.StartScope("public interface IApiContainer");
        scb.AddLine("void SetToken(string token);");
        scb.AddLine("IEventSubscriber EventSubscriber { get; }");
        scb.AddLine("IEventPublisher EventPublisher { get; }");
        scb.AddLine("IEventReceiver EventReceiver { get; }");
        scb.AddLine("ICommandSender Commands { get; }");
        scb.AddLine("IQuerySender Queries { get; }");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope(
            "public class ConsumerApi(IEventReceiver eventReceiver, ICommandSender commands, IQuerySender queries, IEventPublisher eventPublisher, IEventSubscriber eventSubscriber) : IApiContainer");
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("commands.InjectToken(token);");
        scb.AddLine("queries.InjectToken(token);");
        scb.AddLine("eventReceiver.SetToken(token);");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("public IEventReceiver EventReceiver => eventReceiver;");
        scb.AddLine("public ICommandSender Commands => commands;");
        scb.AddLine("public IQuerySender Queries => queries;");
        scb.AddLine("public IEventPublisher EventPublisher => eventPublisher;");
        scb.AddLine("public IEventSubscriber EventSubscriber => eventSubscriber;");
        scb.EndScope();

        return new SourceCodeFile("ApiContainer.g.cs", scb.ToString());
    }
}