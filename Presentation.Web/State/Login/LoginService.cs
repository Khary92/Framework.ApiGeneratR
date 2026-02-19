using Api.Definitions.Generated;
using Presentation.Web.Events;
using Shared.Contracts.EventBus;

namespace Presentation.Web.State.Login;

public class LoginService(
    IEventPublisher eventPublisher,
    QuerySender querySender,
    CommandSender commandSender,
    IWebSocketService webSocketService) : ILoginService
{
    public bool IsUserLoggedIn { get; private set; }
    
    public async Task Login(string token)
    {
        IsUserLoggedIn = true;
        querySender.SetToken(token);
        commandSender.SetToken(token);
        await webSocketService.ConnectAsync(SocketUris.WebSocketUri, token, CancellationToken.None);
        await eventPublisher.PublishAsync(new UserLoggedInEvent());
    }

    public async Task Logout()
    {
        IsUserLoggedIn = false;
        querySender.SetToken(string.Empty);
        commandSender.SetToken(string.Empty);
        await webSocketService.DisposeAsync();
        await eventPublisher.PublishAsync(new UserLoggedOutEvent());
    }
}