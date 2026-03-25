namespace Presentation.Web;

public static class SocketUris
{
    private const string WebSocketBaseAddress = "ws://core-server:8080";
    private static string WebSocketEndpoint => "/ws/events";
    private static string UserChannel => "/userchannel";
    public static Uri WebSocketUri => new(WebSocketBaseAddress + WebSocketEndpoint + UserChannel);
}