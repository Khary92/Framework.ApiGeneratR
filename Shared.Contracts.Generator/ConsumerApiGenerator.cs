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
public class ConsumerApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var apiSourceData = context.GetRequestSourceData();
        var eventSourceData = context.GetEventSourceData();

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = apiSourceData.Combine(assemblyName).Combine(eventSourceData);

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
                        new DiagnosticDescriptor("GEN001", "ServerApiGenerator Error",
                            "Error generating api code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<RequestData> apiDefinitions,
        string? projectNamespace, ImmutableArray<EventSourceData> eventData)
    {
        if (apiDefinitions.IsDefaultOrEmpty || eventData.IsDefaultOrEmpty ||
            projectNamespace is not "Api.Definitions") return;

        ExecuteHttpClientGeneration(context, projectNamespace);
        ExecuteWebsocketInterfaceGeneration(context, projectNamespace);

        ExecuteCommandSenderGeneration(context, apiDefinitions, projectNamespace);
        ExecuteQuerySenderGeneration(context, apiDefinitions, projectNamespace);
        ExecuteWebsocketReceiverGeneration(context, eventData, projectNamespace);
    }

    private static void ExecuteWebsocketReceiverGeneration(SourceProductionContext context,
        ImmutableArray<EventSourceData> eventData, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.WebSockets",
            "System.Text",
            "System.Text.Json",
            "Microsoft.Extensions.Logging",
            "Shared.Contracts.EventBus"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope(
            "public class WebSocketService(global::Microsoft.Extensions.Logging.ILogger<WebSocketService> logger, global::Shared.Contracts.EventBus.IEventPublisher eventPublisher) : IWebSocketService ");
        scb.AddLine("private ClientWebSocket _ws = new ClientWebSocket();");
        scb.AddLine();

        scb.StartScope("public async Task ConnectAsync(Uri uri, string token, CancellationToken cancellationToken)");
        scb.AddLine(
            "if (string.IsNullOrEmpty(token)) throw new InvalidOperationException(\"Token is null or empty.\");");
        scb.AddLine();
        scb.StartScope("if (_ws.State == WebSocketState.Open)");
        scb.AddLine("logger.LogWarning(\"WebSocket is already open\");");
        scb.AddLine("return;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("_ws = new ClientWebSocket();");
        scb.AddLine("_ws.Options.SetRequestHeader(\"Authorization\", $\"Bearer {token}\");");
        scb.AddLine("await _ws.ConnectAsync(uri, CancellationToken.None);");
        scb.AddLine("_ = ReceiveLoop();");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("public async Task DisposeAsync()");
        scb.AddLine("if (_ws.State == WebSocketState.Open)");
        scb.AddIndentedLine(
            "await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, \"Closing\", CancellationToken.None);");
        scb.AddLine("_ws.Dispose();");
        scb.EndScope();
        scb.AddLine();


        scb.StartScope("private async Task ReceiveLoop()");
        scb.AddLine("var buffer = new byte[1024 * 4];");
        scb.StartScope("while (_ws.State == WebSocketState.Open)");
        scb.AddLine("using var ms = new MemoryStream();");
        scb.AddLine("WebSocketReceiveResult result;");
        scb.StartScope("do");
        scb.AddLine("result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);");
        scb.AddLine("ms.Write(buffer, 0, result.Count);");
        scb.EndScope(" while (!result.EndOfMessage);");
        scb.AddLine();
        scb.AddLine("if (result.MessageType == WebSocketMessageType.Close) break;");
        scb.AddLine("if (result.MessageType != WebSocketMessageType.Text) continue;");
        scb.AddLine("ms.Seek(0, SeekOrigin.Begin);");
        scb.AddLine("using var reader = new StreamReader(ms, Encoding.UTF8);");
        scb.AddLine("var message = await reader.ReadToEndAsync();");
        scb.AddLine();
        scb.AddLine("var eventEnvelope = JsonSerializer.Deserialize<EventEnvelope>(message);");
        scb.AddLine("if (eventEnvelope != null) await PublishEvent(eventEnvelope);");
        scb.EndScope();
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("private async Task PublishEvent(EventEnvelope envelope)");
        scb.StartScope("switch (envelope.Type)");

        foreach (var eventType in eventData)
        {
            if (eventType == null) continue;

            scb.AddLine($"case \"{eventType.EventType}\":");
            scb.AddIndentedLine(
                $"await eventPublisher.PublishAsync(JsonSerializer.Deserialize<{eventType.FullTypeName}>(envelope.Payload)!);");
            scb.AddLine("break;");
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        context.AddSource("WebSocketService.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteWebsocketInterfaceGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["System.Net.Http"]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IWebSocketService");
        scb.AddLine("Task ConnectAsync(Uri webSocketUri, string eToken, CancellationToken ctsToken);");
        scb.AddLine("Task DisposeAsync();");
        scb.EndScope();

        context.AddSource("IWebSocketService.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteHttpClientGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings(["System.Net.Http"]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IApiClient");
        scb.AddLine("HttpClient Client { get; }");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public class ApiHttpClient(HttpClient client) : IApiClient");
        scb.AddLine("public HttpClient Client => client;");
        scb.EndScope();

        context.AddSource("ApiClient.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteCommandSenderGeneration(SourceProductionContext context,
        ImmutableArray<RequestData> apiDefinitions, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.Http.Headers",
            "System.Net.Http.Json"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public class CommandSender(IApiClient http)");
        scb.AddLine("private string _token = string.Empty;");
        scb.AddLine();
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("_token = token;");
        scb.EndScope();
        scb.AddLine();

        foreach (var definition in apiDefinitions)
        {
            if (definition == null || definition.CqsType != "Command") continue;

            scb.StartScope(
                $"public async Task<{definition.ReturnValueFullName}> SendAsync({definition.RequestFullName} command, CancellationToken ct = default)");

            if (definition.RequiresAuth)
            {
                scb.AddLine("if (string.IsNullOrEmpty(_token))");
                scb.AddIndentedLine(
                    "throw new InvalidOperationException(\"Token is null or empty. Make sure you are logged in.\");");
                scb.AddLine();
                scb.StartScope($"var httpRequest = new HttpRequestMessage(HttpMethod.Post, \"{definition.Route}\");");
                scb.AddLine("httpRequest.Content = JsonContent.Create(command);");
                scb.EndScope();
                scb.AddLine();
                scb.AddLine("httpRequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", _token);");
                scb.AddLine();
                scb.AddLine("var response = await http.Client.SendAsync(httpRequest, ct);");
                scb.AddLine();
                scb.AddLine("response.EnsureSuccessStatusCode();");
                scb.AddLine(
                    $"return (await response.Content.ReadFromJsonAsync<{definition.ReturnValueFullName}>(ct))!;");
                scb.EndScope();
                scb.AddLine();
                continue;
            }

            scb.AddLine($"var response = await http.Client.PostAsJsonAsync(\"{definition.Route}\", command);");
            scb.AddLine();
            scb.AddLine("response.EnsureSuccessStatusCode();");
            scb.AddLine();
            scb.AddLine($"var result = await response.Content.ReadFromJsonAsync<{definition.ReturnValueFullName}>();");
            scb.AddLine("return result!;");
            scb.EndScope();
        }

        scb.EndScope();

        context.AddSource("CommandSender.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteQuerySenderGeneration(SourceProductionContext context,
        ImmutableArray<RequestData> apiDefinitions, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.Http.Headers",
            "System.Net.Http.Json"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope($"public class QuerySender(IApiClient http)");
        scb.AddLine("private string _token = string.Empty;");
        scb.AddLine();
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("_token = token;");
        scb.EndScope();
        scb.AddLine();

        foreach (var definition in apiDefinitions)
        {
            if (definition == null || definition.CqsType == "Command") continue;

            scb.StartScope(
                $"public async Task<{definition.ReturnValueFullName}> SendAsync({definition.RequestFullName} query, CancellationToken ct = default)");

            if (definition.RequiresAuth)
            {
                scb.AddLine("if (string.IsNullOrEmpty(_token))");
                scb.AddIndentedLine(
                    "throw new InvalidOperationException(\"Token is null or empty. Make sure you are logged in.\");");
                scb.AddLine();
                scb.StartScope($"var httpRequest = new HttpRequestMessage(HttpMethod.Post, \"{definition.Route}\");");
                scb.AddLine("httpRequest.Content = JsonContent.Create(query);");
                scb.EndScope();
                scb.AddLine();
                scb.AddLine("httpRequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", _token);");
                scb.AddLine();
                scb.AddLine("var response = await http.Client.SendAsync(httpRequest, ct);");
                scb.AddLine();
                scb.AddLine("response.EnsureSuccessStatusCode();");
                scb.AddLine(
                    $"return (await response.Content.ReadFromJsonAsync<{definition.ReturnValueFullName}>(ct))!;");
                scb.EndScope();
                scb.AddLine();

                continue;
            }

            scb.AddLine($"var response = await http.Client.PostAsJsonAsync(\"{definition.Route}\", query);");
            scb.AddLine();
            scb.AddLine("response.EnsureSuccessStatusCode();");
            scb.AddLine();
            scb.AddLine($"var result = await response.Content.ReadFromJsonAsync<{definition.ReturnValueFullName}>();");
            scb.AddLine("return result!;");
            scb.EndScope();
            scb.AddLine();
        }

        scb.EndScope();

        context.AddSource("QuerySender.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}