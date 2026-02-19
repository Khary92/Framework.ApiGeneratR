using System.Net.WebSockets;
using Shared.Contracts.EventBus;

namespace Core.Application.Ports;

public interface ISocketConnectionService
{
    Task HandleConnection(string authHeader, WebSocket webSocket);

    Task SendMessageToUser(EventEnvelope envelope, Guid userId, CancellationToken ct = default);

    Task BroadcastToAllUsers(EventEnvelope envelope, CancellationToken ct = default);
}