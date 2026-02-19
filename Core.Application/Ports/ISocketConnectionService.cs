using System.Net.WebSockets;
using Shared.Contracts.EventBus;

namespace Core.Application.Ports;

public interface ISocketConnectionService
{
    Task HandleConnection(string authHeader, WebSocket webSocket);
    Task BroadcastToAllAdmins(EventEnvelope eventEnvelope, CancellationToken ct = default);
}