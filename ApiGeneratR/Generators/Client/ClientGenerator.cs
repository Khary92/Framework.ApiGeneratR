using System;
using System.Collections.Immutable;
using ApiGeneratR.Builder;
using ApiGeneratR.Helpers;
using ApiGeneratR.Helpers.Extractors.Api;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.Generators.Client;

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

        ctx.CreatePartialClasses(consumerData, options);
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

        // API container
        ctx.CreateApiContainer(options, transpilerBuilder, projectNamespace);
        ctx.CreateClientApiExtensions(options, requestData, transpilerBuilder, projectNamespace);

        // Request senders
        ctx.CreateApiClientWithInterface(options, transpilerBuilder, projectNamespace);
        ctx.CreateTokenInjectorBaseClass(options, projectNamespace, transpilerBuilder);
        ctx.CreateAtomicRequestSenderWithInterfaces(requestData, options, transpilerBuilder, projectNamespace);
        ctx.CreateCommandSenderFacade(options, requestData, projectNamespace, transpilerBuilder);
        ctx.CreateQuerySenderFacade(options, requestData, projectNamespace, transpilerBuilder);

        // Websocket
        ctx.GenerateWebsocketReceiver(options, eventData, transpilerBuilder, projectNamespace);
        ctx.CreateWebsocketInterface(options, transpilerBuilder,projectNamespace);

        // EventBus
        ctx.CreateEventBusWithInterfaces(projectNamespace, options, transpilerBuilder);

        //Statics
        ctx.CreateApiDocumentation(projectNamespace, eventData, requestData);
        transpilerBuilder.AddTranspiledDtos(dtoData, eventData, requestData, apiEnumData);
        ctx.CreateTranspilerStatic(transpilerBuilder);
    }
}