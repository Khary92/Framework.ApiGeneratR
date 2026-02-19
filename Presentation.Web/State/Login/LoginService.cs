using Api.Definitions.Generated;
using Api.Definitions.Requests.Queries;
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
    private Guid _userId = Guid.Empty;
    
    public bool IsCurrentUser(Guid userId) => userId == _userId;
    
    public async Task Login(string token)
    {
        IsUserLoggedIn = true;
        querySender.SetToken(token);
        var userIdDto = await querySender.SendAsync(new GetMyUserIdQuery());
        _userId = userIdDto.UserId;
        commandSender.SetToken(token);
        await webSocketService.ConnectAsync(SocketUris.WebSocketUri, token, CancellationToken.None);
        await eventPublisher.PublishAsync(new UserLoggedInEvent());
    }

    public async Task Logout()
    {
        IsUserLoggedIn = false;
        querySender.SetToken(string.Empty);
        commandSender.SetToken(string.Empty);
        _userId = Guid.Empty;
        await webSocketService.DisposeAsync();
        await eventPublisher.PublishAsync(new UserLoggedOutEvent());
    }
}