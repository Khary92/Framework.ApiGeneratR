using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Framework.Reusables.Websocket;

public class WebsocketRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>> _connections = new();

    public void Add(string userId, WebSocket socket)
    {
        var sockets = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<WebSocket, byte>());
        sockets.TryAdd(socket, 0);
    }

    public void Remove(string userId, WebSocket socket)
    {
        if (!_connections.TryGetValue(userId, out var sockets)) return;

        sockets.TryRemove(socket, out _);

        if (sockets.IsEmpty)
        {
            _connections.TryRemove(userId, out _);
        }
    }

    public async Task SendToUserAsync<TEvent>(string userId, string payload, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(userId, out var sockets)) return;

        var buffer = Encoding.UTF8.GetBytes(payload);

        foreach (var socket in sockets.Keys)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
                }
                catch (WebSocketException)
                {
                    // ignore - socket is closed
                }
            }
        }
    }

    public async Task BroadcastAsync(string payload, CancellationToken ct = default)
    {
        var buffer = Encoding.UTF8.GetBytes(payload);

        foreach (var sockets in _connections.Values)
        {
            foreach (var socket in sockets.Keys)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
                    }
                    catch (WebSocketException)
                    {
                        // ignore - socket is closed
                    }
                }
            }
        }
    }
}