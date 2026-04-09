using System;
using System.Collections.Immutable;
using ApiGeneratR.Code;
using ApiGeneratR.Code.Api;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Code.Client;
using ApiGeneratR.Code.Documentation;
using ApiGeneratR.Helpers;
using ApiGeneratR.Helpers.Extractors.Api;
using ApiGeneratR.Helpers.Extractors.Client;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.Generators;

[Generator(LanguageNames.CSharp)]
public class ClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var attributedClientServices = context.GetConsumerApiSourceData();
        var apiSourceData = context.GetRequestSourceData();
        var eventSourceData = context.GetEventSourceData();
        var dtoSourceData = context.GetDtoData();
        var globalOptions = context.GetGlobalOptions();
        var apiEnumSourceData = context.GetApiEnumData();

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
        var combinedApiData = apiSourceData
            .Combine(assemblyName)
            .Combine(eventSourceData)
            .Combine(globalOptions.Combine(dtoSourceData).Combine(apiEnumSourceData));

        context.RegisterSourceOutput(combinedApiData,
            static (spc, source) =>
            {
                try
                {
                    var apiData = source.Left.Left.Left;
                    var assembly = source.Left.Left.Right;
                    var eventData = source.Left.Right;

                    var options = source.Right.Left.Left;
                    var dtos = source.Right.Left.Right;
                    var enums = source.Right.Right;

                    ExecuteApiContainerGeneration(spc, apiData, assembly, eventData, dtos, enums, options);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN002", "Error", ex.Message, "Generator", DiagnosticSeverity.Error,
                            true),
                        Location.None));
                }
            });
    }

    private static void ExecutePartialClientClassGeneration(SourceProductionContext ctx,
        ImmutableArray<ApiConsumerData> consumerData,
        string? nameSpace, GlobalOptions options)
    {
        if (nameSpace == null || !options.IsClientProject(nameSpace)) return;

        ctx.AddFiles(PartialEventReceiverCodeGen.Create(consumerData, options));
    }

    private static void ExecuteApiContainerGeneration(SourceProductionContext ctx,
        ImmutableArray<RequestData> requestData,
        string? projectNamespace, ImmutableArray<EventData> eventData, ImmutableArray<DtoData> dtoData,
        ImmutableArray<ApiEnumData> apiEnumData,
        GlobalOptions options)
    {
        if (requestData.IsDefaultOrEmpty || eventData.IsDefaultOrEmpty ||
            projectNamespace != options.DefinitionsProject) return;

        var transpilerBuilder = new TranspilerBuilder(options);

        if (options.IsTranspilerActive)
        {
            // API container
            transpilerBuilder.AddFile(ApiContainerCodeGen.Create(Language.CSharpTranspiled, projectNamespace));
            transpilerBuilder.AddFile(ApiContainerDependencyInjectionCodeGen.Create(Language.CSharpTranspiled,
                requestData, projectNamespace));

            // Request senders
            transpilerBuilder.AddFile(HttpSenderCodeGen.Create(Language.CSharpTranspiled, projectNamespace));
            transpilerBuilder.AddFile(TokenInjectorBaseCodeGen.Create(Language.CSharpTranspiled, projectNamespace));
            transpilerBuilder.AddFiles(RequestDispatcherCodeGen.Create(Language.CSharpTranspiled, requestData, options,
                projectNamespace));
            transpilerBuilder.AddFiles(
                RequestSenderFacadesCodeGen.Create(Language.CSharpTranspiled, requestData, projectNamespace));

            // Websocket
            transpilerBuilder.AddFile(WebSocketReceiverCodeGen.Create(Language.CSharpTranspiled, options, eventData,
                projectNamespace));

            // Api definitions copy
            transpilerBuilder.AddFiles(ApiClassCodeGen.Create(dtoData, eventData, requestData, apiEnumData));
            transpilerBuilder.AddFile(EventSerializationCodeGen.Create(projectNamespace));

            // EventBus
            transpilerBuilder.AddFile(EventBusCodeGen.Create(Language.CSharpTranspiled, projectNamespace));
        }

        // API container
        ctx.AddFile(ApiContainerCodeGen.Create(Language.CSharpWeb, projectNamespace));
        ctx.AddFile(ApiContainerDependencyInjectionCodeGen.Create(Language.CSharpWeb, requestData, projectNamespace));

        // Request senders
        ctx.AddFile(HttpSenderCodeGen.Create(Language.CSharpWeb, projectNamespace));
        ctx.AddFile(TokenInjectorBaseCodeGen.Create(Language.CSharpWeb, projectNamespace));
        ctx.AddFiles(RequestDispatcherCodeGen.Create(Language.CSharpWeb, requestData, options, projectNamespace));
        ctx.AddFiles(RequestSenderFacadesCodeGen.Create(Language.CSharpWeb, requestData, projectNamespace));

        // Websocket
        ctx.AddFile(WebSocketReceiverCodeGen.Create(Language.CSharpWeb, options, eventData, projectNamespace));

        // EventBus
        ctx.AddFile(EventBusCodeGen.Create(Language.CSharpWeb, projectNamespace));

        // Documentation
        ctx.AddFile(DocumentationCodeGen.Create(Language.CSharpWeb, projectNamespace, eventData, requestData));

        // Transpiler
        ctx.AddFile(transpilerBuilder.GetStaticSourceCode());
    }
}