using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Shared.Contract.Generator.Builder;

namespace Shared.Contract.Generator;

[Generator(LanguageNames.CSharp)]
public class WebSocketGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);
        context.RegisterSourceOutput(assemblyName,
            static (spc, source) =>
            {
                try
                {
                    Execute(spc, source);
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

    private static void Execute(SourceProductionContext context, string? projectNamespace)
    {
        if (projectNamespace is not "Api.Definitions") return;

        ExecuteSocketConnectionServiceGeneration(context, projectNamespace);
        ExecuteExtensionsGeneration(context, projectNamespace);
        ExecuteEnvelopeGeneration(context, projectNamespace);
        ExecuteIdentityRepoGeneration(context, projectNamespace);
    }

    private static void ExecuteIdentityRepoGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IIdentityRepository");
        scb.AddLine("Task<Identity> GetByIdAsync(Guid id);");
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

    private static void ExecuteExtensionsGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "Microsoft.Extensions.DependencyInjection",
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public static class SocketServiceExtensions");
        scb.StartScope("public static void AddDispatcherServices(this IServiceCollection services)");
        scb.AddLine("services.AddSingleton<ISocketConnectionService, SocketConnectionService>();");
        scb.EndScope();
        scb.StartScope("public static void AddWebSocketEndpoints(this WebApplication app)");
        scb.AddLine("var eventWebSocketHandler = app.Services.GetRequiredService<ISocketConnectionService>();");
        scb.AddLine();
        scb.StartScope("app.Map(\"/ws/events\", async context =>");
        scb.StartScope("if (context.WebSockets.IsWebSocketRequest)");
        scb.AddLine("var authHeader = context.Request.Headers[\"Authorization\"].FirstOrDefault();");
        scb.AddLine();
        scb.AddLine("var webSocket = await context.WebSockets.AcceptWebSocketAsync();");
        scb.AddLine();
        scb.AddLine("await eventWebSocketHandler.HandleConnection(authHeader!, webSocket);");
        scb.EndScope();
        scb.StartScope("else");
        scb.AddLine("context.Response.StatusCode = 400;");
        scb.EndScope();
        scb.EndScope(");");
        scb.EndScope();
        scb.EndScope();

        context.AddSource("SocketConnectionExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteSocketConnectionServiceGeneration(SourceProductionContext context,
        string projectNamespace)
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

        scb.StartScope("public interface ISocketConnectionService");
        scb.AddLine("Task HandleConnection(string authHeader, WebSocket webSocket);");
        scb.AddLine("Task SendMessageToUser(EventEnvelope envelope, Guid userId, CancellationToken ct = default);");
        scb.AddLine("Task BroadcastToAllUsers(EventEnvelope envelope, CancellationToken ct = default);");
        scb.EndScope();
        scb.AddLine();
        
        scb.StartScope("public interface IIdentityIdMapper");
        scb.AddLine("Task<string> GetUserIdyByIdentityId(string identityId);");
        scb.EndScope();
        scb.AddLine();
        
        scb.StartScope(
            "public class SocketConnectionService(IIdentityIdMapper db, ILogger<SocketConnectionService> logger, TokenValidationParameters tokenValidationParameters) : ISocketConnectionService");

        scb.AddLine(
            "private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _connections = new();");
        scb.AddLine();

        // HandleConnection Method
        scb.StartScope("public async Task HandleConnection(string authHeader, WebSocket webSocket)");
        scb.StartScope("if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(\"Bearer \"))");
        scb.AddLine(
            "await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, \"Missing or invalid authorization header\", CancellationToken.None);");
        scb.AddLine("return;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("string identityId;");
        scb.AddLine("string role;");
        scb.StartScope("try");
        scb.AddLine("(identityId, role) = ValidateToken(authHeader.Substring(\"Bearer \".Length).Trim());");
        scb.EndScope();
        scb.StartScope("catch");
        scb.AddLine(
            "await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, \"Invalid token\", CancellationToken.None);");
        scb.AddLine("return;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("var userId = await db.GetUserIdyByIdentityId(identityId);");
        scb.AddLine();
        scb.AddLine("if (string.IsNullOrEmpty(userId)) return;");
        scb.AddLine();
        scb.AddLine("var connectionKey = $\"{role}:{userId}\";");
        scb.AddLine(
            "var userSockets = _connections.GetOrAdd(connectionKey, _ => new ConcurrentDictionary<WebSocket, byte>());");
        scb.AddLine("userSockets.TryAdd(webSocket, 0);");
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
        scb.StartScope("if (_connections.TryGetValue(connectionKey, out var userSocketsToClean))");
        scb.AddLine("userSocketsToClean.TryRemove(webSocket, out _);");
        scb.AddLine();
        scb.AddLine("if (userSocketsToClean.IsEmpty) _connections.TryRemove(connectionKey, out _);");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("if (webSocket.State != WebSocketState.Aborted)");
        scb.AddLine(
            "await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, \"Closed\", CancellationToken.None);");
        scb.EndScope();
        scb.EndScope();
        scb.EndScope();
        scb.AddLine();

        scb.StartScope(
            "public async Task SendMessageToUser(EventEnvelope envelope, Guid userId, CancellationToken ct = default)");
        scb.AddLine("if (!_connections.TryGetValue($\"user:{userId}\", out var userSockets)) return;");
        scb.AddLine();
        scb.AddLine("var json = JsonSerializer.Serialize(envelope);");
        scb.AddLine("var bytes = Encoding.UTF8.GetBytes(json);");
        scb.AddLine("var segment = new ArraySegment<byte>(bytes);");
        scb.AddLine();
        scb.AddLine("var tasks = userSockets.Keys");
        scb.AddLine("    .Where(s => s.State == WebSocketState.Open)");
        scb.AddLine("    .Select(async socket =>");
        scb.AddLine("    {");
        scb.AddLine("        try");
        scb.AddLine("        {");
        scb.AddLine("            await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);");
        scb.AddLine("        }");
        scb.AddLine("        catch (Exception ex)");
        scb.AddLine("        {");
        scb.AddLine(
            "            logger.LogWarning(ex, \"Could not send message to specific socket for user {UserId}\", userId);");
        scb.AddLine("        }");
        scb.AddLine("    });");
        scb.AddLine();
        scb.AddLine("await Task.WhenAll(tasks);");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope(
            "public async Task BroadcastToAllUsers(EventEnvelope eventEnvelope, CancellationToken ct = default)");
        scb.AddLine("var openSockets = _connections");
        scb.AddLine("    .Where(kvp => kvp.Key.StartsWith(\"user:\"))");
        scb.AddLine("    .SelectMany(kvp => kvp.Value.Keys)");
        scb.AddLine("    .Where(socket => socket.State == WebSocketState.Open)");
        scb.AddLine("    .ToList();");
        scb.AddLine();
        scb.AddLine("if (openSockets.Count == 0) return;");
        scb.AddLine();
        scb.AddLine("var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventEnvelope));");
        scb.AddLine("var segment = new ArraySegment<byte>(bytes);");
        scb.AddLine();
        scb.AddLine("var tasks = openSockets.Select(async socket =>");
        scb.AddLine("{");
        scb.AddLine("    try");
        scb.AddLine("    {");
        scb.AddLine("        if (socket.State == WebSocketState.Open)");
        scb.AddLine("            await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);");
        scb.AddLine("    }");
        scb.AddLine("    catch (Exception ex)");
        scb.AddLine("    {");
        scb.AddLine("        logger.LogWarning(ex, \"Broadcast failed for one socket.\");");
        scb.AddLine("    }");
        scb.AddLine("});");
        scb.AddLine();
        scb.AddLine("await Task.WhenAll(tasks);");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("private (string userId, string role) ValidateToken(string token)");
        scb.AddLine("var tokenHandler = new JwtSecurityTokenHandler();");
        scb.AddLine("var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);");
        scb.AddLine();
        scb.AddLine("var identityId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        scb.AddLine("if (string.IsNullOrEmpty(identityId))");
        scb.AddLine("    throw new SecurityTokenException(\"Token does not contain user ID\");");
        scb.AddLine();
        scb.AddLine("var role = principal.FindFirst(ClaimTypes.Role)?.Value;");
        scb.AddLine();
        scb.AddLine("return string.IsNullOrEmpty(role)");
        scb.AddLine("    ? throw new SecurityTokenException(\"Token does not contain role\")");
        scb.AddLine("    : (userId: identityId, role);");
        scb.EndScope();

        scb.EndScope();

        context.AddSource("SocketConnectionService.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}