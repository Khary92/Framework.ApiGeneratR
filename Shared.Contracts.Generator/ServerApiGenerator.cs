using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Shared.Contract.Generator.Builder;
using Shared.Contract.Generator.Helpers;
using Shared.Contract.Generator.Mapper;

namespace Shared.Contract.Generator;

[Generator(LanguageNames.CSharp)]
public class ServerApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var apiSourceData = context.GetRequestSourceData();

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = apiSourceData.Combine(assemblyName);

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
                        new DiagnosticDescriptor("GEN001", "ServerApiGenerator Error",
                            "Error generating api code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<RequestData> apiDefinitions,
        string? projectNamespace)
    {
        if (apiDefinitions.IsDefaultOrEmpty) return;

        if (projectNamespace is not "Api.Definitions") return;

        GenerateEndpoints(context, apiDefinitions, projectNamespace);
    }

    private static void GenerateEndpoints(SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["Microsoft.AspNetCore.Builder", "System.Security.Claims", "Microsoft.AspNetCore.Http"]);
        scb.SetNamespace($"{projectNamespace.Replace(".", "")}.Generated");

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

        scb.StartScope("public static void AddApiEndpoints(this WebApplication app)");

        if (!requests.IsDefaultOrEmpty)
            foreach (var request in requests)
            {
                if (request == null) continue;

                if (request is { RequiresAuth: true })
                {
                    scb.StartScope(
                        $"app.MapPost(\"{request.Route}\", async ({request.RequestFullName} request, global::Shared.Contracts.Mediator.IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>");
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
                    $"app.MapPost(\"{request.Route}\", async ({request.RequestFullName} request, global::Shared.Contracts.Mediator.IMediator mediator) =>");
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