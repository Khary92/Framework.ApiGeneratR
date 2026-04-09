using System;
using System.Collections.Immutable;
using ApiGeneratR.Code;
using ApiGeneratR.Code.Api;
using ApiGeneratR.Code.Server;
using ApiGeneratR.Helpers;
using ApiGeneratR.Helpers.Extractors.Api;
using ApiGeneratR.Helpers.Extractors.Server;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.Generators.Server;

[Generator(LanguageNames.CSharp)]
public class ServerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var events = context.GetEventSourceData();
        var requests = context.GetRequestSourceData();
        var requestHandlers = context.GetRequestHandlerSourceData();

        context.RegisterSourceOutput(events.Combine(assemblyName).Combine(context.GetGlobalOptions()),
            static (spc, source) =>
            {
                try
                {
                    spc.AddFiles(WebSocketDependencyInjectionCodeGen.Create(source.Left.Left, source.Left.Right,
                        source.Right));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("WEBSOCKGEN001", "Generator crashed", "{0}", "Generator",
                            DiagnosticSeverity.Error, true), Location.None, ex.Message));
                }
            });

        context.RegisterSourceOutput(
            requestHandlers.Combine(assemblyName).Combine(context.GetGlobalOptions()),
            static (spc, source) =>
            {
                try
                {
                    CreateSourceMediator(spc, source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("WEBSOCKGEN001", "Generator crashed", "{0}", "Generator",
                            DiagnosticSeverity.Error, true), Location.None, ex.Message));
                }
            });

        context.RegisterSourceOutput(requests.Combine(assemblyName).Combine(context.GetGlobalOptions()),
            static (spc, source) =>
            {
                try
                {
                    CreateEndpoints(spc, source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "Server Api Generator Error",
                            "Error generating server api code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });


        var combined = assemblyName.Combine(context.GetGlobalOptions());

        context.RegisterSourceOutput(combined,
            static (spc, combined) =>
            {
                try
                {
                    CreateWebSockets(spc, combined.Left, combined.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "WebSocketGenerator Error",
                            "Error generating websocket code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void CreateWebSockets(SourceProductionContext context, string? projectNamespace,
        GlobalOptions options)
    {
        if (projectNamespace != options.DefinitionsProject) return;

        context.AddFile(WebSocketCodeGen.CreateSocketConnectionService(projectNamespace, options));
        context.AddFile(WebSocketCodeGen.CreateExtensions(projectNamespace, options));
        context.AddFile(EventSerializationCodeGen.Create(projectNamespace));
        context.AddFiles(WebSocketCodeGen.CreateInterface(projectNamespace, options));
    }

    private static void CreateSourceMediator(SourceProductionContext context,
        ImmutableArray<RequestHandlerData> requestHandlerData, string? projectNamespace, GlobalOptions options)
    {
        if (requestHandlerData.IsDefaultOrEmpty) return;
        if (projectNamespace == null || projectNamespace != options.HandlerProject) return;

        context.AddFile(MediatorCodeGen.Create(requestHandlerData, projectNamespace, options));
        context.AddFile(MediatorDependencyInjectionCodeGen.Create(requestHandlerData, projectNamespace, options));
    }

    private static void CreateEndpoints(SourceProductionContext context,
        ImmutableArray<RequestData> requests, string? projectNamespace, GlobalOptions options)
    {
        if (requests.IsDefaultOrEmpty) return;
        if (projectNamespace != options.DefinitionsProject) return;

        context.AddFile(MinimalApiEndpointsCodeGen.Create(requests, projectNamespace, options));
        context.AddFiles(MediatorInterfaceCodeGen.Create(requests, projectNamespace));
    }
}