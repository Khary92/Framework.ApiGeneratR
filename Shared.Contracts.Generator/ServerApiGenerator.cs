using System;
using System.Collections.Generic;
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

        ExecuteDocumentationGeneration(context, apiDefinitions, projectNamespace);
        ExecuteCodeGeneration(context, apiDefinitions, projectNamespace);
    }

    private static void ExecuteDocumentationGeneration(SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace)
    {
        var mdb = new MarkdownBuilder();
        mdb.AddHeader("API Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the available endpoints. Total endpoints: {requests.Length}");

        if (requests.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            mdb.AddHeader("Endpoints Overview", 2);

            var rows = new List<List<string>>();
            foreach (var handler in requests)
                rows.Add([
                    $"`{handler.HttpMethod}`", $"{handler.RequiresAuth}", $"`{handler.Route}`",
                    handler.RequestShortName,
                    handler.DataStructureType
                ]);

            mdb.AddTable(new List<string> { "Method", "Requires Auth", "Route", "Command/Record", "Type" }, rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Request Definitions", 2);

            foreach (var definition in requests)
            {
                if (definition == null) continue;

                mdb.AddHeader(definition.RequestShortName, 3);
                mdb.AddParagraph($"Full Type: `{definition.RequestFullName}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {definition.RequestShortName}");

                foreach (var member in definition.Members) mdb.AddLine(member);

                mdb.EndCodeBlock();
            }

            foreach (var request in requests)
            {
                mdb.StartCodeBlock();
                mdb.AddLine(
                    $"public async Task<CommandResponse> SendAsync({request.RequestFullName} command, CancellationToken ct = default)");
                mdb.AddLine("{");
                mdb.AddLine("        var token = loginService.Token;");
                mdb.AddLine("        if (string.IsNullOrEmpty(token))");
                mdb.AddLine(
                    "            throw new InvalidOperationException(\"Token is null or empty. Make sure you are logged in.\");");
                mdb.AddLine();
                mdb.AddLine(
                    $"        var httpRequest = new HttpRequestMessage(HttpMethod.{request.HttpMethod}, \"{request.Route}\");");
                mdb.AddLine("         {");
                mdb.AddLine("            httpRequest.Content = JsonContent.Create(command);");
                mdb.AddLine("         };");
                mdb.AddLine();
                mdb.AddLine(
                    "         httpRequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", token);");
                mdb.AddLine();
                mdb.AddLine("         var response = await adminApi.Client.SendAsync(httpRequest, ct);");
                mdb.AddLine("         response.EnsureSuccessStatusCode();");
                mdb.AddLine();
                mdb.AddLine("        return (await response.Content.ReadFromJsonAsync<CommandResponse>(ct))!;");
                mdb.AddLine("}");
                mdb.EndCodeBlock();
            }
        }
        
        SourceCodeBuilder scb = new();
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public class ApiDocumentation");
        scb.AddLine();

        scb.AddLine("public string FileName => \"ApiDocumentation.md\";");
        scb.AddLine("public string Markdown => \"\"\"");

        var lines = mdb.ToString().Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines) scb.AddLine(line);

        scb.AddLine("\"\"\";");
        scb.EndScope();

        context.AddSource($"{projectNamespace.Replace(".", "")}ApiDocumentation.g.cs",
            SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteCodeGeneration(SourceProductionContext context,
        ImmutableArray<RequestData> apiDefinitions, string projectNamespace)
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

        if (!apiDefinitions.IsDefaultOrEmpty)
            foreach (var definition in apiDefinitions)
            {
                if (definition == null) continue;

                if (definition is { RequiresAuth: true })
                {
                    scb.StartScope(
                        $"app.MapPost(\"{definition.Route}\", async ({definition.RequestFullName} request, global::Shared.Contracts.Mediator.IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>");
                    scb.AddLine("if (!user.IsValidUser()) return Results.Unauthorized();");
                    scb.AddLine();
                    var mediatorDelegate = $"{(definition.RequestHasIdentityId
                            ? "var result = await mediator.HandleAsync(request with { IdentityId = user.GetIdentityId() }, ct);"
                            : "var result = await mediator.HandleAsync(request, ct);"
                        )}";
                    scb.AddLine(mediatorDelegate);
                    scb.AddLine();
                    scb.AddLine("return result is not null");
                    scb.AddIndentedLine("? Results.Ok(result)");
                    scb.AddIndentedLine(": Results.NotFound();");
                    scb.EndScope($"){(definition.RequiresAuth ? ".RequireAuthorization()" : string.Empty)};");
                    scb.AddLine();
                    continue;
                }

                scb.StartScope(
                    $"app.MapPost(\"{definition.Route}\", async ({definition.RequestFullName} request, global::Shared.Contracts.Mediator.IMediator mediator) =>");
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