using System;
using System.Collections.Immutable;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Client;

public static class ApiContainerDependencyInjectionCodeGen
{
    public static SourceCodeFile Create(Language targetLanguage, ImmutableArray<RequestData> requests, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return CreateCSharpClientApiExtensions(requests, projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return CreateCSharpClientApiExtensions(requests, TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile CreateCSharpClientApiExtensions(ImmutableArray<RequestData> requests,
        string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Threading",
            "Microsoft.Extensions.DependencyInjection"
        ]);

        scb.SetNamespace(projectNamespace);

        scb.StartScope("public static class ApiFacadeExtensions");
        scb.StartScope(
            "public static void AddGeneratedClientApiServices(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)");

        scb.AddLine("services.Add(new ServiceDescriptor(typeof(IApiContainer), typeof(ConsumerApi), lifetime));");
        scb.AddLine(
            "services.Add(new ServiceDescriptor(typeof(IEventReceiver), typeof(WebSocketService), lifetime));");
        scb.AddLine(
            "services.Add(new ServiceDescriptor(typeof(ICommandSender), typeof(GeneratedCommandSender), lifetime));");
        scb.AddLine(
            "services.Add(new ServiceDescriptor(typeof(IQuerySender), typeof(GeneratedQuerySender), lifetime));");
        scb.AddLine("services.Add(new ServiceDescriptor(typeof(EventService), typeof(EventService), lifetime));");
        scb.AddLine(
            "services.Add(new ServiceDescriptor(typeof(IEventSubscriber), sp => sp.GetRequiredService<EventService>(), lifetime));");
        scb.AddLine(
            "services.Add(new ServiceDescriptor(typeof(IEventPublisher), sp => sp.GetRequiredService<EventService>(), lifetime));");

        foreach (var request in requests)
        {
            if (request == null) continue;

            scb.AddLine(
                $"services.Add(new ServiceDescriptor(typeof(I{request.RequestShortName}Sender), typeof(Generated{request.RequestShortName}Sender), lifetime));");
        }

        scb.EndScope();
        scb.EndScope();

        return new SourceCodeFile("ApiContainerExtensions.g.cs", scb.ToString());
    }
}