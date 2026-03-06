using System;
using System.Collections.Immutable;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.CodeGen.Generators.Client;

[Generator(LanguageNames.CSharp)]
public class ClientApiInjectorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var attributedClientServices = context.GetConsumerApiSourceData();
        var apiSourceData = context.GetRequestSourceData();
        var eventSourceData = context.GetEventSourceData();
        var globalOptions = context.GetGlobalOptions();

        context.RegisterSourceOutput(attributedClientServices.Combine(assemblyName).Combine(globalOptions),
            static (spc, source) =>
            {
                try
                {
                    ExecutePartialClientClassGeneration(spc, source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "Api Injection Generator Error",
                            "Error generating api injection code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });

        context.RegisterSourceOutput(
            apiSourceData.Combine(assemblyName).Combine(eventSourceData).Combine(globalOptions),
            static (spc, source) =>
            {
                try
                {
                    ExecuteApiContainerGeneration(spc, source.Left.Left.Left, source.Left.Left.Right, source.Left.Right,
                        source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN002", "Api Container Generator Error",
                            "Error generating api container code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void ExecutePartialClientClassGeneration(SourceProductionContext ctx,
        ImmutableArray<ApiConsumerData> consumerData,
        string? nameSpace, GlobalOptions options)
    {
        if (nameSpace == null || !options.IsClientProject(nameSpace)) return;

        ctx.CreatePartialClasses(consumerData);
    }

    private static void ExecuteApiContainerGeneration(SourceProductionContext ctx,
        ImmutableArray<RequestData> requestData,
        string? projectNamespace, ImmutableArray<EventSourceData> eventData, GlobalOptions options)
    {
        if (requestData.IsDefaultOrEmpty || eventData.IsDefaultOrEmpty ||
            projectNamespace != options.DefinitionsProject) return;
        
        // API container
        ctx.CreateApiContainer(projectNamespace);
        ctx.CreateClientApiExtensions(requestData, projectNamespace);

        // Request senders
        ctx.CreateApiClientWithInterface(projectNamespace);
        ctx.CreateTokenInjectorBaseClass(projectNamespace);
        ctx.CreateAtomicRequestSenderWithInterfaces(requestData, projectNamespace);
        ctx.CreateCommandSenderWithInterface(requestData, projectNamespace);
        ctx.CreateQuerySenderWithInterface(requestData, projectNamespace);

        // Websocket
        ctx.GenerateWebsocketReceiver(eventData, projectNamespace);
        ctx.CreateWebsocketInterface(projectNamespace);

        // EventBus
        ctx.CreateEventBusWithInterfaces(projectNamespace, options);
        
        //Documentation
        ctx.CreateDocumentation(projectNamespace, eventData, requestData);
    }
}