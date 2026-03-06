using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

public static class ServerApiExtensions
{
        public static void CreateSourceMediator(this SourceProductionContext context,
        ImmutableArray<MediatorHandlerData> handlers, string projectNamespace, GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);

        if (options.IsLogMediator) scb.AddUsing("Microsoft.Extensions.Logging");

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public class SourceMediator : ApiGeneratR.Definitions.Mediator.IMediator");

        scb.AddLine(
            "private readonly Dictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();");
        scb.AddLine("private readonly IServiceProvider _serviceProvider;");
        scb.AddLine();

        if (options.IsLogMediator)
        {
            scb.AddLine($"private readonly {options.GetLoggerForType("SourceMediator")} _logger;");
            scb.AddLine();
        }

        var optionalLogger = options.IsLogMediator ? $", {options.GetLoggerForType("SourceMediator")} logger" : "";

        scb.StartScope($"public SourceMediator(IServiceProvider serviceProvider{optionalLogger})");
        scb.AddLine("_serviceProvider = serviceProvider;");
        if (options.IsLogMediator) scb.AddLine("_logger = logger;");
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

            if (options.IsLogMediator)
                scb.AddLine($"_logger.LogDebug(\"Registered handler: {handler.HandlerShortName}\");");

            if (!Equals(handler, handlers.Last())) scb.AddLine();
        }

        if (options.IsLogMediator)
            scb.AddLine(
                $"_logger.LogInformation(\"ApiGeneratR Mediator initialized. Registered {handlers.Length} handlers.\");");

        scb.EndScope();
        scb.AddLine();

        scb.StartScope(
            "public async Task<TResponse> HandleAsync<TResponse>(ApiGeneratR.Definitions.Mediator.IRequest<TResponse> request, CancellationToken ct = default)");

        scb.AddLine("if (request == null) throw new ArgumentNullException(\"request is null\");");

        if (options.IsLogMediator)
        {
            scb.AddLine();
            scb.AddLine("_logger.LogDebug($\"Handling incoming {request}\");");
        }

        scb.AddLine();

        if (options.IsLogMediator)
        {
            scb.StartScope("if (!_handlers.TryGetValue(request.GetType(), out var handlerWrapper))");
            scb.AddLine("_logger.LogDebug($\"No handler registered for {request.GetType()} \");");
            scb.AddLine("throw new Exception($\"Handler for type {request.GetType()} not found\");");
            scb.EndScope();
        }
        else
        {
            scb.AddLine("if (!_handlers.TryGetValue(request.GetType(), out var handlerWrapper))");
            scb.AddIndentedLine("throw new Exception($\"Handler for type {request.GetType()} not found\");");
        }

        scb.AddLine();
        scb.StartScope("try");
        scb.AddLine("var result = (TResponse)await handlerWrapper(request, ct);");
        if (options.IsLogMediator)
            scb.AddLine("_logger.LogDebug(\"Successfully handled {RequestType}\", request.GetType().Name);");
        scb.AddLine("return result;");
        scb.EndScope();

        scb.StartScope(options.IsLogMediator ? "catch (Exception e)" : "catch (Exception)");
        
        if (options.IsLogMediator)
            scb.AddLine("_logger.LogError(e, \"Error handling request {RequestType}\", request.GetType().Name);");
        scb.AddLine("throw;");
        scb.EndScope();
        scb.EndScope();

        scb.EndScope();

        context.AddSource("SourceMediator.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
        
    public static void CreateMediatorExtensions(this SourceProductionContext context,
        ImmutableArray<MediatorHandlerData> handlers, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
            "Microsoft.Extensions.DependencyInjection"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public static class MediatorExtensions");
        scb.StartScope("extension(IServiceCollection services)");
        scb.StartScope("public void AddGeneratedMediator()");
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
    
    public static void CreateWebsocketsExtensions(this SourceProductionContext ctx,
        ImmutableArray<EventSourceData> events, string? projectNamespace, GlobalOptions options)
    {
        if (events.IsDefaultOrEmpty) return;

        if (projectNamespace != options.DefinitionsProject) return;

        foreach (var @event in events)
        {
            if (@event == null) continue;

            var scb = new SourceCodeBuilder();

            scb.SetUsings(["System.Text.Json"]);

            scb.SetNamespace($"{projectNamespace}.Generated");

            scb.StartScope($"public static class {@event.TypeName}WebsocketExtensions");

            scb.StartScope(
                $"public static global::ApiGeneratR.Definitions.Generated.EventEnvelope ToWebsocketMessage(this {@event.FullTypeName} @event)");
            scb.AddLine(
                $"return new(\"{@event.EventType}\", JsonSerializer.Serialize(@event), DateTime.UtcNow);");
            scb.EndScope();
            scb.EndScope();

            ctx.AddSource($"{@event.TypeName}WebsocketsExtensions.g.cs",
                SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
    
     public static void CreateEndpoints(this SourceProductionContext context,
        ImmutableArray<RequestData> requests, string? projectNamespace, GlobalOptions options)
    {
        if (requests.IsDefaultOrEmpty) return;
        if (projectNamespace != options.DefinitionsProject) return;
        
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "Microsoft.AspNetCore.Builder", "System.Security.Claims", "Microsoft.AspNetCore.Http"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public static class ApiExtensions");

        scb.StartScope("extension(ClaimsPrincipal user)");

        scb.StartScope("public bool IsValidUser()");
        scb.AddLine("var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        scb.AddLine("var role = user.FindFirst(ClaimTypes.Role)?.Value;");
        scb.AddLine("return !string.IsNullOrEmpty(userIdString) && !string.IsNullOrEmpty(role) && role == \"user\";");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("public Guid GetIdentityId()");
        scb.AddLine("var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        scb.AddLine("return Guid.TryParse(userIdString, out var id) ? id : Guid.Empty;");
        scb.EndScope();

        scb.EndScope();
        scb.AddLine();

        scb.StartScope("public static void MapGeneratedApiEndpoints(this WebApplication app)");

        if (!requests.IsDefaultOrEmpty)
            foreach (var request in requests)
            {
                if (request == null) continue;

                if (request is { RequiresAuth: true })
                {
                    scb.StartScope(
                        $"app.MapPost(\"{request.Route}\", async ({request.RequestFullName} request, global::ApiGeneratR.Definitions.Mediator.IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>");
                    scb.AddLine("if (!user.IsValidUser()) return Results.Unauthorized();");
                    scb.AddLine();
                    var mediatorDelegate = $"{(request.RequestHasIdentityId
                            ? "var result = await mediator.HandleAsync(request with { IdentityId = user.GetIdentityId() }, ct);"
                            : "var result = await mediator.HandleAsync(request, ct);"
                        )}";
                    scb.AddLine(mediatorDelegate);
                    scb.AddLine();
                    scb.AddLine("return result is not null");
                    scb.AddIndentedLine("? Results.Ok(result)");
                    scb.AddIndentedLine(": Results.NotFound();");
                    scb.EndScope($"){(request.RequiresAuth ? ".RequireAuthorization()" : string.Empty)};");
                    scb.AddLine();
                    continue;
                }

                scb.StartScope(
                    $"app.MapPost(\"{request.Route}\", async ({request.RequestFullName} request, global::ApiGeneratR.Definitions.Mediator.IMediator mediator) =>");
                scb.AddLine("var result = await mediator.HandleAsync(request);");
                scb.AddLine();
                scb.AddLine("return result is not null");
                scb.AddIndentedLine("? Results.Ok(result)");
                scb.AddIndentedLine(": Results.NotFound();");
                scb.EndScope(");");
            }

        scb.EndScope();
        scb.EndScope();

        context.AddSource($"{projectNamespace.Replace(".", "")}ApiExtensions.g.cs",
            SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}