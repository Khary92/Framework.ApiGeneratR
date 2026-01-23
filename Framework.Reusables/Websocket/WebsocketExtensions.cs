using System.Net.WebSockets;

namespace Framework.Reusables.Websocket;

public static class WebsocketExtensions
{
    public static void AddWebsocket(this WebApplication app)
    {
        app.UseWebSockets();

        var registry = app.Services.GetRequiredService<WebsocketRegistry>();

        app.Map("/ws", async (HttpContext context, CancellationToken cancellationToken) =>
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var userId = context.Request.Query["userId"].ToString() ?? "anonymous";

            registry.Add(userId, webSocket);
            app.Logger.LogInformation("WebSocket connected: {UserId}", userId);

            try
            {
                var buffer = new byte[1024];
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType != WebSocketMessageType.Close) continue;
                    
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client",
                        cancellationToken);
                    break;
                }
            }
            catch (WebSocketException ex)
            {
                app.Logger.LogError(ex, "WebSocket error for user {UserId}", userId);
            }
            finally
            {
                registry.Remove(userId, webSocket);
                if (webSocket.State != WebSocketState.Closed)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }

                app.Logger.LogInformation("WebSocket disconnected: {UserId}", userId);
            }
        });
    }
}