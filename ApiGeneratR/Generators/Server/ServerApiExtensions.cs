using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Generators.Server;

public static class ServerApiExtensions
{
    public static void CreateSourceMediator(this SourceProductionContext context,
        ImmutableArray<RequestHandlerData> handlers, string projectNamespace, GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);

        if (options.IsLogMediator) scb.AddUsing("Microsoft.Extensions.Logging");

        scb.SetNamespace($"{projectNamespace}.Generated");

        var parameters = string.Empty;
        foreach (var handler in handlers)
        {
            parameters += handler == handlers.Last()
                ? $"global::{options.DefinitionsProject}.Generated.I{handler.RequestShortName}Handler {handler.HandlerShortName.ToLower()}"
                : $"global::{options.DefinitionsProject}.Generated.I{handler.RequestShortName}Handler {handler.HandlerShortName.ToLower()}, ";
        }

        var optionalLogger = options.IsLogMediator ? $"{options.GetLoggerForType("SourceMediator")} logger, " : "";

        scb.StartScope(
            $"public class SourceMediator({optionalLogger}{parameters}) : global::{options.DefinitionsProject}.Generated.IMediator");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            scb.StartScope(
                $"public async Task<{handler.ResponseFullName}> HandleAsync({handler.RequestFullName} request, CancellationToken ct = default)");

            scb.AddLine("if (request == null) throw new ArgumentNullException(\"request is null\");");

            if (options.IsLogMediator)
                scb.AddLine("logger.LogDebug($\"Handling request: {request} \");");
            scb.AddLine();
            scb.StartScope("try");
            scb.AddLine($"var result = await {handler.HandlerShortName.ToLower()}.HandleAsync(request, ct);");
            if (options.IsLogMediator)
                scb.AddLine("logger.LogDebug(\"Successfully handled {RequestFullName}\", request.GetType().Name);");
            scb.AddLine("return result;");
            scb.EndScope();

            scb.StartScope(options.IsLogMediator ? "catch (Exception e)" : "catch (Exception)");

            if (options.IsLogMediator)
                scb.AddLine(
                    "logger.LogError(e, \"Error handling request {RequestFullName}\", request.GetType().Name);");
            scb.AddLine("throw;");
            scb.EndScope();
            scb.EndScope();

            if (!Equals(handler, handlers.Last())) scb.AddLine();
        }

        scb.EndScope();

        context.AddSource("SourceMediator.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    public static void CreateMediatorExtensions(this SourceProductionContext context,
        ImmutableArray<RequestHandlerData> handlers, string? projectNamespace, GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
            "Microsoft.Extensions.DependencyInjection"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public static class MediatorExtensions");
        scb.StartScope("extension(IServiceCollection services)");
        scb.StartScope("public void AddGeneratedHandlerServices(ServiceLifetime lifetime = ServiceLifetime.Scoped)");
        scb.AddLine(
            $"services.Add(new ServiceDescriptor(typeof(global::{options.DefinitionsProject}.Generated.IMediator), typeof(global::{projectNamespace}.Generated.SourceMediator), lifetime));");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            scb.AddLine(
                $"services.Add(new ServiceDescriptor(typeof(global::{options.DefinitionsProject}.Generated.I{handler.RequestShortName}Handler), typeof({handler.HandlerFullName}), lifetime));");
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
                $"public static global::{options.DefinitionsProject}.Generated.EventEnvelope ToWebsocketMessage(this {@event.FullTypeName} @event)");
            scb.AddLine(
                $"return new(\"{@event.EventType}\", JsonSerializer.Serialize(@event), DateTime.UtcNow);");
            scb.EndScope();
            scb.EndScope();

            ctx.AddSource($"{@event.TypeName}WebsocketsExtensions.g.cs",
                SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }

    public static void CreateMediatorInterface(this SourceProductionContext ctx,
        ImmutableArray<RequestData> requests, string? projectNamespace)
    {
        var mediatorInterfaces = string.Empty;
        foreach (var request in requests)
        {
            if (request == null) continue;

            var scb = new SourceCodeBuilder();

            scb.SetNamespace($"{projectNamespace}.Generated");

            scb.StartScope($"public interface I{request.RequestShortName}Handler");
            scb.AddLine(
                $"Task<{request.ReturnValueFullName}> HandleAsync({request.RequestFullName} request, CancellationToken ct = default);");
            scb.EndScope();

            ctx.AddSource($"I{request.RequestShortName}Handler.g.cs",
                SourceText.From(scb.ToString(), Encoding.UTF8));

            mediatorInterfaces += requests.Last() == request
                ? $"I{request.RequestShortName}Handler;"
                : $"I{request.RequestShortName}Handler, ";
        }

        var mscb = new SourceCodeBuilder();

        mscb.SetNamespace($"{projectNamespace}.Generated");

        mscb.AddLine("public interface IMediator : " + mediatorInterfaces);

        ctx.AddSource("IMediator.g.cs",
            SourceText.From(mscb.ToString(), Encoding.UTF8));
    }

    public static void CreateEndpoints(this SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace, GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "Microsoft.AspNetCore.Builder", "System.Security.Claims", "Microsoft.AspNetCore.Http"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public static class ApiExtensions");

        scb.StartScope("public static Guid GetIdentityId(this ClaimsPrincipal user)");
        scb.AddLine("var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        scb.AddLine("return Guid.TryParse(userIdString, out var id) ? id : Guid.Empty;");
        scb.EndScope();

        scb.AddLine();

        scb.StartScope("public static void MapGeneratedApiEndpoints(this WebApplication app)");

        if (!requests.IsDefaultOrEmpty)
            foreach (var request in requests)
            {
                if (request == null) continue;

                scb.StartScope(
                    $"app.MapPost(\"{request.Route}\", async ({request.RequestFullName} request, global::{options.DefinitionsProject}.Generated.IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>");
                var mediatorDelegate = $"{(request.RequestHasIdentityId
                        ? "var result = await mediator.HandleAsync(request with { IdentityId = user.GetIdentityId() }, ct);"
                        : "var result = await mediator.HandleAsync(request, ct);"
                    )}";
                scb.AddLine(mediatorDelegate);
                scb.AddLine();
                scb.AddLine("return result is not null");
                scb.AddIndentedLine("? Results.Ok(result)");
                scb.AddIndentedLine(": Results.NotFound();");
                scb.EndScope(
                    $"){(request.AuthPolicy == "AllowAnonymous" ? ".AllowAnonymous()" : $".RequireAuthorization(\"{request.AuthPolicy}\")")};");
                scb.AddLine();
            }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("ApiEndpointExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}