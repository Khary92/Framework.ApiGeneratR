using System.Collections.Immutable;
using System.Text;
using Framework.Generators.Builder;
using Framework.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Framework.Generators.Generators;

[Generator(LanguageNames.CSharp)]
public class MediatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlers = context.GetInterfaceImplementingClassSymbols("IRequestHandler");

        context.RegisterSourceOutput(handlers,
            static (spc, h) => Execute(spc, h));

        context.RegisterSourceOutput(handlers,
            static (spc, h) => ExecuteDocuGeneration(spc, h));
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> handlers)
    {
        CreateSourceMediator(context, handlers);
        CreateExtensionMethod(context, handlers);
    }

    private static void CreateExtensionMethod(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> handlers)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System", "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
            "Microsoft.Extensions.DependencyInjection"
        ]);
        scb.SetNamespace("Framework.Generated");
        scb.StartScope("public static class MediatorExtensions");
        scb.StartScope("extension(IServiceCollection services)");
        scb.StartScope("public void AddSingletonMediatorServices()");
        scb.AddLine(
            "services.AddSingleton<global::Framework.Contract.Documentation.IDocumentation, MediatorDocumentation>();");
        scb.AddLine(
            "services.AddSingleton<global::Framework.Contract.Documentation.IDocumentation, ApiDocumentation>();");
        scb.AddLine("services.AddSingleton<global::Framework.Contract.Mediator.IMediator, SourceMediator>();");
        if (!handlers.IsDefaultOrEmpty)
        {
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var interfaceSymbol = handler.AllInterfaces.FirstOrDefault(i =>
                    i.Name == "IRequestHandler" &&
                    i.ContainingNamespace.ToDisplayString().Contains("Framework.Contract"));

                if (interfaceSymbol == null || interfaceSymbol.TypeArguments.Length != 2) continue;

                var requestType = interfaceSymbol.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var responseType = interfaceSymbol.TypeArguments[1]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var handlerType = handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                scb.AddLine(
                    $"services.AddSingleton<global::Framework.Contract.Mediator.IRequestHandler<{requestType}, {responseType}>, {handlerType}>();");
            }
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        context.AddSource("MediatorExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void CreateSourceMediator(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> handlers)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System", "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);
        scb.SetNamespace("Framework.Generated");

        scb.StartScope("public class SourceMediator : global::Framework.Contract.Mediator.IMediator");

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

        if (!handlers.IsDefaultOrEmpty)
        {
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var interfaceSymbol = handler.AllInterfaces.FirstOrDefault(i =>
                    i.Name == "IRequestHandler" &&
                    i.ContainingNamespace.ToDisplayString().Contains("Framework.Contract"));

                if (interfaceSymbol == null || interfaceSymbol.TypeArguments.Length != 2) continue;

                var requestType = interfaceSymbol.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var handlerType = handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                scb.StartScope($"_handlers.Add(typeof({requestType}), async (req, ct) =>");
                scb.AddLine($"var handler = (_serviceProvider.GetService(typeof({handlerType})) as {handlerType})");
                scb.AddIndentedLine($"?? ActivatorUtilities.CreateInstance<{handlerType}>(_serviceProvider);");
                scb.AddLine($"var result = await handler.HandleAsync(({requestType})req);");
                scb.AddLine("return (object)result;");
                scb.EndScope(");");

                if (!SymbolEqualityComparer.Default.Equals(handler, handlers.Last()))
                {
                    scb.AddLine();
                }
            }
        }

        scb.EndScope();
        scb.AddLine();

        scb.StartScope(
            "public async Task<TResponse> HandleAsync<TResponse>(global::Framework.Contract.Mediator.IRequest<TResponse> request, CancellationToken ct = default)");
        scb.AddLine("if (request == null) throw new ArgumentNullException(\"request is null\");");
        scb.AddLine();
        scb.AddLine("if (!_handlers.TryGetValue(request.GetType(), out var handlerWrapper))");
        scb.AddIndentedLine("throw new Exception($\"Handler for type {request.GetType()} not found\");");
        scb.AddLine();
        scb.AddLine("return (TResponse)await handlerWrapper(request, ct);");
        scb.EndScope();

        scb.EndScope();

        context.AddSource("SourceMediator.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteDocuGeneration(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> handlers)
    {
        var mdb = new MarkdownBuilder();

        mdb.AddHeader("Mediator Registry Documentation");
        mdb.AddParagraph(
            "This document lists all automatically registered Request Handlers and their associated types.");

        if (handlers.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No Request Handlers found in the solution._");
        }
        else
        {
            mdb.AddHeader("Registered Handlers", 2);

            var rows = new List<List<string>>();
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var interfaceSymbol = handler.AllInterfaces.FirstOrDefault(i =>
                    i.Name == "IRequestHandler" &&
                    i.ContainingNamespace.ToDisplayString().Contains("Framework.Contract"));

                if (interfaceSymbol == null || interfaceSymbol.TypeArguments.Length != 2) continue;

                var requestName = interfaceSymbol.TypeArguments[0].Name;
                var responseName = interfaceSymbol.TypeArguments[1].Name;
                var handlerName = handler.Name;

                rows.Add([handlerName, requestName, responseName]);
            }

            mdb.AddTable(["Handler Class", "Request Type", "Response Type"], rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Detailed Handler Mapping", 2);

            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var interfaceSymbol = handler.AllInterfaces.FirstOrDefault(i =>
                    i.Name == "IRequestHandler" &&
                    i.ContainingNamespace.ToDisplayString().Contains("Framework.Contract"));

                if (interfaceSymbol == null) continue;

                mdb.AddHeader(handler.Name, 3);
                mdb.AddListItem($"**Handler:** `{handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}`",
                    0);
                mdb.AddListItem(
                    $"**Request:** `{interfaceSymbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}`",
                    0);
                mdb.AddListItem(
                    $"**Response:** `{interfaceSymbol.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}`",
                    0);
                mdb.AddLine();
            }
        }

        SourceCodeBuilder scb = new();
        scb.SetNamespace("Framework.Generated");
        scb.StartScope("public class MediatorDocumentation : global::Framework.Contract.Documentation.IDocumentation");
        scb.AddLine();

        scb.AddLine("public string FileName => \"MediatorDocumentation.md\";");
        scb.AddLine("public string Markdown => \"\"\"");

        var lines = mdb.ToString().Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            scb.AddLine(line);
        }

        scb.AddLine("\"\"\";");
        scb.EndScope();

        context.AddSource("MediatorDocumentation.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}