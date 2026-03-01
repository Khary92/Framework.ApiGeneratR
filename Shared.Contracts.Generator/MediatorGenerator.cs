using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Shared.Contract.Generator.Builder;
using Shared.Contract.Generator.Helpers;
using Shared.Contract.Generator.Mapper;

namespace Shared.Contract.Generator;

[Generator(LanguageNames.CSharp)]
public class MediatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlers = context.GetMediatorRequestHandlers();

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = handlers.Combine(assemblyName);

        context.RegisterSourceOutput(combined,
            static (spc, source) =>
            {
                try
                {
                    Execute(spc, source.Left, source.Right);
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
        string? projectNamespace)
    {
        switch (projectNamespace)
        {
            case "Api.Definitions":
                CreateInterfaces(context, projectNamespace);
                break;
            case "Core.Application":
                CreateSourceMediator(context, handlers, "Api.Definitions");
                CreateExtensionMethod(context, handlers, "Api.Definitions");
                break;
        }
    }

    private static void CreateInterfaces(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public interface IMediator");
        scb.AddLine(
            "Task<TResponse> HandleAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);");
        scb.EndScope();

        context.AddSource("IMediator.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));

        scb = new SourceCodeBuilder();
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope(
            "public interface IRequestHandler<in TRequestIn, TResponseOut> where TRequestIn : IRequest<TResponseOut>");
        scb.AddLine(
            "Task<TResponseOut> HandleAsync(TRequestIn request, CancellationToken cancellationToken = default);");
        scb.EndScope();

        context.AddSource("IRequestHandler.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));

        scb = new SourceCodeBuilder();
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.AddLine("public interface IRequest<TResult>;");

        context.AddSource("IRequest.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
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
            "services.AddSingleton<IMediator, SourceMediator>();");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            scb.AddLine(
                $"services.AddSingleton<IRequestHandler<{handler.RequestType}, {handler.ResponseType}>, {handler.HandlerType}>();");
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        context.AddSource($"{projectNamespace}MediatorExtensions.g.cs",
            SourceText.From(scb.ToString(), Encoding.UTF8));
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

        scb.StartScope("public class SourceMediator : IMediator");

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
            "public async Task<TResponse> HandleAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)");
        scb.AddLine("if (request == null) throw new ArgumentNullException(\"request is null\");");
        scb.AddLine();
        scb.AddLine("if (!_handlers.TryGetValue(request.GetType(), out var handlerWrapper))");
        scb.AddIndentedLine("throw new Exception($\"Handler for type {request.GetType()} not found\");");
        scb.AddLine();
        scb.AddLine("return (TResponse)await handlerWrapper(request, ct);");
        scb.EndScope();

        scb.EndScope();

        context.AddSource($"{projectNamespace}SourceMediator.g.cs",
            SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}