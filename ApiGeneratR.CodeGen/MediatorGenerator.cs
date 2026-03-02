using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

[Generator(LanguageNames.CSharp)]
public class MediatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlers = context.GetMediatorRequestHandlers();

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = handlers.Combine(assemblyName).Combine(context.GetGlobalOptions());

        context.RegisterSourceOutput(combined,
            static (spc, source) =>
            {
                try
                {
                    Execute(spc, source.Left.Left, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "MediatorGenerator Error",
                            "Error generating mediator code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<MediatorHandlerData> handlers,
        string? projectNamespace, GlobalOptions options)
    {
        if (projectNamespace != options.HandlerProject) return;
        
        CreateSourceMediator(context, handlers, options.DefinitionsProject);
        CreateExtensionMethod(context, handlers, options.DefinitionsProject);
    }

    private static void CreateExtensionMethod(SourceProductionContext context,
        ImmutableArray<MediatorHandlerData> handlers, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System", "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
            "Microsoft.Extensions.DependencyInjection"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public static class MediatorExtensions");
        scb.StartScope("extension(IServiceCollection services)");
        scb.StartScope("public void AddSingletonMediatorServices()");
        scb.AddLine(
            "services.AddSingleton<ApiGeneratR.Definitions.Mediator.IMediator, SourceMediator>();");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            scb.AddLine(
                $"services.AddSingleton<ApiGeneratR.Definitions.Mediator.IRequestHandler<{handler.RequestType}, {handler.ResponseType}>, {handler.HandlerType}>();");
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        context.AddSource("MediatorExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void CreateSourceMediator(SourceProductionContext context,
        ImmutableArray<MediatorHandlerData> handlers, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System", "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public class SourceMediator : ApiGeneratR.Definitions.Mediator.IMediator");

        scb.AddLine(
            "private readonly Dictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();");
        scb.AddLine("private readonly IServiceProvider _serviceProvider;");
        scb.AddLine();

        scb.StartScope("public SourceMediator(IServiceProvider serviceProvider)");
        scb.AddLine("_serviceProvider = serviceProvider;");
        scb.AddLine("RegisterHandlers();");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("private void RegisterHandlers()");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            scb.StartScope($"_handlers.Add(typeof({handler.RequestType}), async (req, ct) =>");
            scb.AddLine(
                $"var handler = (_serviceProvider.GetService(typeof({handler.HandlerType})) as {handler.HandlerType})");
            scb.AddIndentedLine($"?? ActivatorUtilities.CreateInstance<{handler.HandlerType}>(_serviceProvider);");
            scb.AddLine($"var result = await handler.HandleAsync(({handler.RequestType})req);");
            scb.AddLine("return (object)result;");
            scb.EndScope(");");

            if (!Equals(handler, handlers.Last())) scb.AddLine();
        }

        scb.EndScope();
        scb.AddLine();

        scb.StartScope(
            "public async Task<TResponse> HandleAsync<TResponse>(ApiGeneratR.Definitions.Mediator.IRequest<TResponse> request, CancellationToken ct = default)");
        scb.AddLine("if (request == null) throw new ArgumentNullException(\"request is null\");");
        scb.AddLine();
        scb.AddLine("if (!_handlers.TryGetValue(request.GetType(), out var handlerWrapper))");
        scb.AddIndentedLine("throw new Exception($\"Handler for type {request.GetType()} not found\");");
        scb.AddLine();
        scb.AddLine("return (TResponse)await handlerWrapper(request, ct);");
        scb.EndScope();

        scb.EndScope();

        context.AddSource("SourceMediator.g.cs",
            SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}