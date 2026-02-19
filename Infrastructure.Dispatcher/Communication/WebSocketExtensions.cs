using Core.Application.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dispatcher.Communication;

public static class WebSocketExtensions
{
    public static void AddAdminWebSocketEndpoints(this WebApplication app)
    {
        var eventWebSocketHandler = app.Services.GetRequiredService<ISocketConnectionService>();

        app.Map(SocketUri.WebSocketEndpoint, async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await eventWebSocketHandler.HandleConnection(authHeader!, webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });
    }
}