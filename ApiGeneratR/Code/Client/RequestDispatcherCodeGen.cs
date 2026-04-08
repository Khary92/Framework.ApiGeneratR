using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ApiGeneratR.Builder;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Client;

public static class RequestDispatcherCodeGen
{
    public static List<SourceCodeFile> Create(Language targetLanguage, ImmutableArray<RequestData> requests,
        GlobalOptions options, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return CreateCSharpRequestSenderWithInterfaces(requests, options, projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return CreateCSharpRequestSenderWithInterfaces(requests, options,
                    TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static List<SourceCodeFile> CreateCSharpRequestSenderWithInterfaces(ImmutableArray<RequestData> requests,
        GlobalOptions options, string projectNamespace)
    {
        var result = new List<SourceCodeFile>();

        foreach (var request in requests)
        {
            var scb = new SourceCodeBuilder();
            scb.SetUsings([
                "System.Net.Http.Headers",
                "System.Net.Http.Json"
            ]);

            if (options.IsLogApiClient) scb.AddUsing("Microsoft.Extensions.Logging");

            scb.SetNamespace(projectNamespace);

            scb.StartScope($"public interface I{request.RequestShortName}Sender");
            scb.AddLine(
                $"Task<{request.ReturnValueFullName}> SendAsync({request.RequestFullName} {request.CqsType.ToLower()}, CancellationToken ct = default);");
            scb.EndScope();
            scb.AddLine();

            scb.StartScope(
                $"public class Generated{request.RequestShortName}Sender(IApiClient http{(options.IsLogApiClient ? $", ILogger<Generated{request.RequestShortName}Sender> logger" : string.Empty)}) : TokenInjection, I{request.RequestShortName}Sender");

            scb.StartScope(
                $"public async Task<{request.ReturnValueFullName}> SendAsync({request.RequestFullName} command, CancellationToken ct = default)");

            if (options.IsLogApiClient)
            {
                scb.AddLine("logger.LogDebug(\"Sending request \" + command.ToString());");
                scb.AddLine();
            }

            if (request.AuthPolicy != "AllowAnonymous")
            {
                scb.AddLine("if (string.IsNullOrEmpty(Token))");
                scb.AddIndentedLine(
                    "throw new InvalidOperationException(\"Token is null or empty. Make sure you are logged in.\");");
                scb.AddLine();
                scb.StartScope(
                    $"var httpRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, \"{request.Route}\");");
                scb.AddLine("httpRequest.Content = JsonContent.Create(command);");
                scb.EndScope();
                scb.AddLine();
                scb.AddLine("httpRequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", Token);");
                scb.AddLine();
                scb.AddLine("var response = await http.Client.SendAsync(httpRequest, ct);");
                scb.AddLine();
                scb.AddLine("response.EnsureSuccessStatusCode();");
                scb.AddLine(
                    $"return (await response.Content.ReadFromJsonAsync<{request.ReturnValueFullName}>(ct))!;");
                scb.EndScope();
            }
            else
            {
                scb.AddLine($"var response = await http.Client.PostAsJsonAsync(\"{request.Route}\", command);");
                scb.AddLine();
                scb.AddLine("response.EnsureSuccessStatusCode();");
                scb.AddLine();
                scb.AddLine($"var result = await response.Content.ReadFromJsonAsync<{request.ReturnValueFullName}>();");
                scb.AddLine("return result!;");
                scb.EndScope();
            }

            scb.EndScope();

            result.Add(new SourceCodeFile($"Generated{request.RequestShortName}Sender.g.cs", scb.ToString()));
        }

        return result;
    }
}