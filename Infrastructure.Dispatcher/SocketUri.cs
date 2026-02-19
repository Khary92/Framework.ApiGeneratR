namespace Infrastructure.Dispatcher;

public static class SocketUri
{
    private const string WebSocketBaseAddress = "ws://core-server:8080";
    public static string WebSocketEndpoint => "/ws/events";
    public static Uri WebSocketUri => new(WebSocketBaseAddress + WebSocketEndpoint);
}