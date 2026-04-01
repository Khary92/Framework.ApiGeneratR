using System;
using System.Linq;
using System.Text;
using ApiGeneratR.Builder;
using ApiGeneratR.Helpers;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Generators.Server;

[Generator(LanguageNames.CSharp)]
public class WebSocketGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = assemblyName.Combine(context.GetGlobalOptions());

        context.RegisterSourceOutput(combined,
            static (spc, combined) =>
            {
                try
                {
                    Execute(spc, combined.Left, combined.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "WebSocketGenerator Error",
                            "Error generating websocket code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void Execute(SourceProductionContext context, string? projectNamespace, GlobalOptions options)
    {
        if (projectNamespace != options.DefinitionsProject) return;

        ExecuteSocketConnectionServiceGeneration(context, projectNamespace, options);
        ExecuteExtensionsGeneration(context, projectNamespace, options);
        ExecuteEnvelopeGeneration(context, projectNamespace);
        ExecuteInterfaceGeneration(context, projectNamespace, options);
    }

    private static void ExecuteInterfaceGeneration(SourceProductionContext context, string projectNamespace,
        GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Collections.Concurrent",
            "System.IdentityModel.Tokens.Jwt",
            "System.Net.WebSockets",
            "System.Security.Claims",
            "System.Text",
            "System.Text.Json",
            "Microsoft.Extensions.Logging",
            "Microsoft.IdentityModel.Tokens"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IEventSender");
        foreach (var channel in options.CommunicationChannels)
        {
            scb.AddLine($"Task Handle{channel.Channel}ConnectionAsync(WebSocket webSocket, string identityId);");
            scb.AddLine($"Task SendTo{channel.Channel}Async(EventEnvelope envelope, CancellationToken ct = default);");
        }
        scb.AddLine("Task SendToIdAsync(EventEnvelope envelope, Guid targetId, CancellationToken ct = default);");
        scb.AddLine("Task BroadcastAsync(EventEnvelope envelope, CancellationToken ct = default);");
        scb.EndScope();
        scb.AddLine();

        context.AddSource("IEventSender.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));

        scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Collections.Concurrent",
            "System.IdentityModel.Tokens.Jwt",
            "System.Net.WebSockets",
            "System.Security.Claims",
            "System.Text",
            "System.Text.Json",
            "Microsoft.Extensions.Logging",
            "Microsoft.IdentityModel.Tokens"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IIdentityIdMapper");
        scb.AddLine("Task<string> GetUserIdyByIdentityId(string identityId);");
        scb.EndScope();
        scb.AddLine();

        context.AddSource("IIdentityIdMapper.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteEnvelopeGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public class EventEnvelope(string type, string payload, DateTime timestamp)");
        scb.AddLine("public string Type { get; set; } = type;");
        scb.AddLine("public string Payload { get; set; } = payload;");
        scb.AddLine("public DateTime Timestamp { get; set; } = timestamp;");
        scb.EndScope();

        context.AddSource("EventEnvelope.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteExtensionsGeneration(SourceProductionContext context, string projectNamespace,
        GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Http",
            "System.Linq",
            "System.Security.Claims"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");
        
        scb.StartScope("public static class SocketServiceExtensions");
        scb.AddLine();
        scb.AddLine();
        scb.StartScope("public static void AddGeneratedSocketConnectionService(this IServiceCollection services)");
        scb.AddLine("services.AddSingleton<IEventSender, SocketConnectionService>();");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public static void MapGeneratedWebSocketEndpoint(this WebApplication app)");
        scb.AddLine("var eventWebSocketHandler = app.Services.GetRequiredService<IEventSender>();");
        scb.AddLine();
        
        foreach (var channel in options.CommunicationChannels)
        {
            if (!options.AuthProfiles.Contains(channel.AuthProfile))
                throw new Exception("Auth profile not found for channel: " + channel.Channel);
            
            scb.StartScope($"app.MapGet(\"/ws/events/{channel.Channel.ToLower()}\", async context =>");
            scb.StartScope("if (context.WebSockets.IsWebSocketRequest)");
            scb.AddLine();
            scb.AddLine("var webSocket = await context.WebSockets.AcceptWebSocketAsync();");
            scb.AddLine();
            scb.AddLine($"await eventWebSocketHandler.Handle{channel.Channel}ConnectionAsync(webSocket, context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);");
            scb.EndScope();
            scb.StartScope("else");
            scb.AddLine("context.Response.StatusCode = 400;");
            scb.EndScope();
            scb.EndScope($").RequireAuthorization(\"{channel.AuthProfile}\");");
        }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("SocketConnectionExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteSocketConnectionServiceGeneration(SourceProductionContext context,
        string projectNamespace, GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Collections.Concurrent",
            "System.IdentityModel.Tokens.Jwt",
            "System.Net.WebSockets",
            "System.Security.Claims",
            "System.Text",
            "System.Text.Json",
            "Microsoft.Extensions.Logging",
            "Microsoft.IdentityModel.Tokens"
        ]);

        if (options.IsLogWebsockets) scb.AddUsing("Microsoft.Extensions.Logging");

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope(
            "public class SocketConnectionService(IServiceProvider serviceProvider, ILogger<SocketConnectionService> logger) : IEventSender");

        scb.AddLine(
            "private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _connectionsById = new();");
        
        foreach (var channel in options.CommunicationChannels)
        {
            scb.AddLine();
            scb.AddLine(
                $"private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _connections{channel.Channel} = new();");

            scb.AddLine();

            scb.StartScope(
                $"public async Task Handle{channel.Channel}ConnectionAsync(WebSocket webSocket, string identityId)");
            scb.AddLine();
            scb.AddLine("using var scope = serviceProvider.CreateScope();");
            scb.AddLine("var db = scope.ServiceProvider.GetRequiredService<IIdentityIdMapper>();");
            scb.AddLine("var userId = await db.GetUserIdyByIdentityId(identityId);");
            scb.AddLine();
            scb.AddLine("if (string.IsNullOrEmpty(userId)) return;");
            scb.AddLine();
            scb.AddLine("var connectionKey = $\"{userId}\";");
            scb.AddLine(
                "var userSockets = _connectionsById.GetOrAdd(connectionKey, _ => new ConcurrentDictionary<WebSocket, byte>());");
            scb.AddLine("userSockets.TryAdd(webSocket, 0);");
            scb.AddLine();
            scb.AddLine(
                $"var {channel.Channel.ToLower()}Sockets = _connections{channel.Channel}.GetOrAdd(connectionKey, _ => new ConcurrentDictionary<WebSocket, byte>());");
            scb.AddLine($"{channel.Channel.ToLower()}Sockets.TryAdd(webSocket, 0);");
            scb.AddLine();
            scb.AddLine("var buffer = new byte[1024 * 4];");
            scb.StartScope("try");
            scb.StartScope("while (webSocket.State == WebSocketState.Open)");
            scb.AddLine(
                "var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);");
            scb.AddLine("if (result.MessageType == WebSocketMessageType.Close) break;");
            scb.EndScope();
            scb.EndScope();
            scb.StartScope("catch (Exception)");
            scb.AddLine("logger.LogError(\"Websocket connection was interrupted\");");
            scb.EndScope();
            scb.StartScope("finally");
            scb.StartScope("if (_connectionsById.TryGetValue(connectionKey, out var userSocketsToClean))");
            scb.AddLine("userSocketsToClean.TryRemove(webSocket, out _);");
            scb.AddLine();
            scb.AddLine("if (userSocketsToClean.IsEmpty) _connectionsById.TryRemove(connectionKey, out _);");
            scb.EndScope();
            scb.AddLine();
            scb.StartScope(
                $"if (_connections{channel.Channel}.TryGetValue(connectionKey, out var {channel.Channel}SocketsToClean))");
            scb.AddLine($"{channel.Channel}SocketsToClean.TryRemove(webSocket, out _);");
            scb.AddLine();
            scb.AddLine(
                $"if ({channel.Channel}SocketsToClean.IsEmpty) _connections{channel.Channel}.TryRemove(connectionKey, out _);");
            scb.EndScope();
            scb.AddLine();
            scb.StartScope("if (webSocket.State != WebSocketState.Aborted)");
            scb.AddLine(
                "await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, \"Closed\", CancellationToken.None);");
            scb.EndScope();
            scb.EndScope();
            scb.EndScope();
            
            scb.StartScope(
                $"public async Task SendTo{channel.Channel}Async(EventEnvelope eventEnvelope, CancellationToken ct = default)");
            scb.AddLine($"var openSockets = _connections{channel.Channel}");
            scb.AddIndentedLine(".SelectMany(kvp => kvp.Value.Keys)");
            scb.AddIndentedLine(".Where(socket => socket.State == WebSocketState.Open)");
            scb.AddIndentedLine(".ToList();");
            scb.AddLine();
            scb.AddLine("if (openSockets.Count == 0) return;");
            scb.AddLine();
            scb.AddLine("var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventEnvelope));");
            scb.AddLine("var segment = new ArraySegment<byte>(bytes);");
            scb.AddLine();
            scb.StartScope("var tasks = openSockets.Select(async socket =>");
            scb.StartScope("try");
            scb.StartScope("if (socket.State == WebSocketState.Open)");
            scb.AddLine("await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);");
            scb.EndScope();
            scb.EndScope();
            scb.StartScope("catch (Exception ex)");
            scb.AddLine("logger.LogWarning(ex, \"Broadcast failed for one socket.\");");
            scb.EndScope();
            scb.EndScope(");");
            scb.AddLine();
            scb.AddLine("await Task.WhenAll(tasks);");
            scb.EndScope();
        }

        scb.AddLine();

        scb.StartScope(
            "public async Task SendToIdAsync(EventEnvelope envelope, Guid userId, CancellationToken ct = default)");
        scb.AddLine("if (!_connectionsById.TryGetValue($\"{userId}\", out var userSockets)) return;");
        scb.AddLine();
        scb.AddLine("var json = JsonSerializer.Serialize(envelope);");
        scb.AddLine("var bytes = Encoding.UTF8.GetBytes(json);");
        scb.AddLine("var segment = new ArraySegment<byte>(bytes);");
        scb.AddLine();
        scb.StartScope(
            "var tasks = userSockets.Keys.Where(s => s.State == WebSocketState.Open).Select(async socket =>");
        scb.StartScope("try");
        scb.AddLine("await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);");
        scb.EndScope();
        scb.StartScope("catch (Exception ex)");
        scb.AddLine("logger.LogWarning(ex, \"Could not send message to specific socket for user {UserId}\", userId);");
        scb.EndScope();
        scb.EndScope(");");
        scb.AddLine();
        scb.AddLine("await Task.WhenAll(tasks);");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope(
            "public async Task BroadcastAsync(EventEnvelope eventEnvelope, CancellationToken ct = default)");
        foreach (var channel in options.CommunicationChannels)
        {
            scb.AddLine($"await SendTo{channel.Channel}Async(eventEnvelope, ct);");
        }
        scb.EndScope();
        scb.EndScope();
        scb.AddLine();
        
        context.AddSource("SocketConnectionService.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}