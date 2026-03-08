using System;
using System.Collections.Immutable;
using ApiGeneratR.Helpers;
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
                    spc.CreateWebsocketsExtensions(source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("WEBSOCKGEN001", "Generator crashed", "{0}", "Generator",
                            DiagnosticSeverity.Error, true), Location.None, ex.Message));
                }
            });

        context.RegisterSourceOutput(
            requestHandlers.Combine(events).Combine(assemblyName).Combine(context.GetGlobalOptions()),
            static (spc, source) =>
            {
                try
                {
                    spc.CreateSourceMediator(source.Left.Left.Left, source.Left.Right, source.Right);
                    spc.CreateMediatorExtensions(source.Left.Left.Left, source.Left.Right, source.Right);
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
    }

    private static void CreateEndpoints(SourceProductionContext context,
        ImmutableArray<RequestData> requests, string? projectNamespace, GlobalOptions options)
    {
        if (requests.IsDefaultOrEmpty) return;
        if (projectNamespace != options.DefinitionsProject) return;

        context.CreateEndpoints(requests, projectNamespace, options);
        context.CreateMediatorInterface(requests, projectNamespace);
    }
}