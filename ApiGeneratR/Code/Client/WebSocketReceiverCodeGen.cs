using System;
using System.Collections.Immutable;
using ApiGeneratR.Builder;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Client;

public static class WebSocketReceiverCodeGen
{
    public static SourceCodeFile Create(Language targetLanguage, GlobalOptions options,
        ImmutableArray<EventData> events, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return GenerateCsharpWebsocketReceiver(options, events, projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return GenerateCsharpWebsocketReceiver(options, events,
                    TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile GenerateCsharpWebsocketReceiver(GlobalOptions options,
        ImmutableArray<EventData> events, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.WebSockets",
            "System.Text",
            "System.Text.Json",
            "Microsoft.Extensions.Logging"
        ]);

        scb.SetNamespace(projectNamespace);
        
        scb.StartScope("public interface IEventReceiver");
        scb.AddLine("void SetToken(string _token);");
        scb.AddLine("Task ConnectAsync(Uri webSocketUri, CancellationToken ctsToken);");
        scb.AddLine("Task DisposeAsync();");
        scb.EndScope();
        
        scb.StartScope(
            "public class WebSocketService(global::Microsoft.Extensions.Logging.ILogger<WebSocketService> logger, IEventPublisher eventPublisher) : IEventReceiver ");

        scb.AddLine("private string _token;");
        scb.AddLine();
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("_token = token;");
        scb.EndScope();

        scb.AddLine("private ClientWebSocket _ws = new ClientWebSocket();");
        scb.AddLine();

        scb.StartScope("public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)");
        scb.AddLine(
            "if (string.IsNullOrEmpty(_token)) throw new InvalidOperationException(\"Token is null or empty.\");");
        scb.AddLine();
        scb.StartScope("if (_ws.State == WebSocketState.Open)");
        scb.AddLine("logger.LogWarning(\"WebSocket is already open\");");
        scb.AddLine("return;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("_ws = new ClientWebSocket();");
        scb.AddLine("_ws.Options.SetRequestHeader(\"Authorization\", $\"Bearer {_token}\");");
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

        if (options.IsLogApiClient)
        {
            scb.StartScope("if (eventEnvelope == null)");
            scb.AddLine("logger.LogDebug(\"Received an event but deserialization failed\");");
            scb.AddLine("return;");
            scb.EndScope();
            scb.AddLine();
            scb.AddLine("await PublishEvent(eventEnvelope);");
        }
        else
        {
            scb.AddLine("if (eventEnvelope != null) await PublishEvent(eventEnvelope);");
        }

        scb.EndScope();
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("private async Task PublishEvent(EventEnvelope envelope)");
        scb.StartScope("switch (envelope.Type)");

        foreach (var eventType in events)
        {
            if (eventType == null) continue;

            scb.AddLine($"case \"{eventType.EventType}\":");
            if (options.IsLogApiClient)
            {
                scb.AddIndentedLine($"logger.LogDebug(\"Received an event of type {eventType.EventType}\");");
            }

            scb.AddIndentedLine(
                $"await eventPublisher.PublishAsync(JsonSerializer.Deserialize<{eventType.FullTypeName}>(envelope.Payload)!);");
            scb.AddLine("break;");
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        return new SourceCodeFile("EventReceiver.g.cs", scb.ToString());
    }
}