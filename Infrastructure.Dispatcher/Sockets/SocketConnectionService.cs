using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Core.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.EventBus;

namespace Infrastructure.Dispatcher.Sockets;

public class SocketConnectionService(
    IUnitOfWork db,
    ILogger<SocketConnectionService> logger,
    TokenValidationParameters tokenValidationParameters)
    : ISocketConnectionService
{
 private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _connections = new();

    public async Task HandleConnection(string authHeader, WebSocket webSocket)
    {
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Missing or invalid authorization header",
                CancellationToken.None);
            return;
        }

        string identityId;
        string role;
        try
        {
            (identityId, role) = ValidateToken(authHeader.Substring("Bearer ".Length).Trim());
        }
        catch
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token", CancellationToken.None);
            return;
        }

        var user = db.Users.FirstOrDefault(u => u.IdentityId == Guid.Parse(identityId));

        if (user == null) return;

        var connectionKey = $"{role}:{user.Id}";
        var userSockets = _connections.GetOrAdd(connectionKey, _ => new ConcurrentDictionary<WebSocket, byte>());
        userSockets.TryAdd(webSocket, 0);

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch (Exception)
        {
            logger.LogError("Websocket connection was interrupted");
        }
        finally
        {
            if (_connections.TryGetValue(connectionKey, out var userSocketsToClean))
            {
                userSocketsToClean.TryRemove(webSocket, out _);

                if (userSocketsToClean.IsEmpty) _connections.TryRemove(connectionKey, out _);
            }

            if (webSocket.State != WebSocketState.Aborted)
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }
    }

    public async Task SendMessageToUser(EventEnvelope envelope, Guid userId,
        CancellationToken ct = default)
    {
        if (!_connections.TryGetValue($"user:{userId}", out var userSockets)) return;

        var json = JsonSerializer.Serialize(envelope);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var tasks = userSockets.Keys
            .Where(s => s.State == WebSocketState.Open)
            .Select(async socket =>
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not send message to specific socket for user {UserId}", userId);
                }
            });

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastToAllUsers(EventEnvelope eventEnvelope, CancellationToken ct = default)
    {
        var openSockets = _connections
            .Where(kvp => kvp.Key.StartsWith("user:"))
            .SelectMany(kvp => kvp.Value.Keys)
            .Where(socket => socket.State == WebSocketState.Open)
            .ToList();

        if (openSockets.Count == 0) return;

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventEnvelope));
        var segment = new ArraySegment<byte>(bytes);

        var tasks = openSockets.Select(async socket =>
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Broadcast failed for one socket.");
            }
        });

        await Task.WhenAll(tasks);
    }

    private (string userId, string role) ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);

        var identityId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identityId))
            throw new SecurityTokenException("Token does not contain user ID");

        var role = principal.FindFirst(ClaimTypes.Role)?.Value;

        return string.IsNullOrEmpty(role)
            ? throw new SecurityTokenException("Token does not contain role")
            : (userId: identityId, role);
    }
}