using System.Collections.Immutable;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Server;

public static class MinimalApiEndpointsCodeGen
{
    public static SourceCodeFile Create(ImmutableArray<RequestData> requests, string projectNamespace,
        GlobalOptions options)
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

        return new SourceCodeFile("ApiEndpointExtensions.g.cs", scb.ToString());
    }
}