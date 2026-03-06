using System;
using System.Collections.Immutable;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.CodeGen.Generators.Server;

[Generator(LanguageNames.CSharp)]
public class MediatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var requestHandlerImplementations = context.GetMediatorRequestHandlers();
        var events = context.GetEventSourceData();
        var requests = context.GetRequestSourceData();

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

        context.RegisterSourceOutput(requests.Combine(assemblyName).Combine(context.GetGlobalOptions()),
            static (spc, source) =>
            {
                try
                {
                    spc.CreateEndpoints(source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "Server Api Generator Error",
                            "Error generating server api code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });

        context.RegisterSourceOutput(
            requestHandlerImplementations.Combine(assemblyName).Combine(context.GetGlobalOptions()),
            static (spc, source) =>
            {
                try
                {
                    ExecuteMediatorCreation(spc, source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "Mediator Generator Error",
                            "Error generating mediator code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void ExecuteMediatorCreation(SourceProductionContext ctx,
        ImmutableArray<MediatorHandlerData> requestHandlerImplementations, string? projectNamespace,
        GlobalOptions options)
    {
        if (projectNamespace != options.HandlerProject) return;

        ctx.CreateSourceMediator(requestHandlerImplementations, options.DefinitionsProject, options);
        ctx.CreateMediatorExtensions(requestHandlerImplementations, options.DefinitionsProject);
    }
}