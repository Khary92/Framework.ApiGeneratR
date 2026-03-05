using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Requests.Queries;
using Presentation.Web.Events;

namespace Presentation.Web.State.Login;

public class LoginService(
    IApiFacade api,
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
        // Prepare API
        api.SetToken(token);
        await api.WebSocket.ConnectAsync(SocketUris.WebSocketUri, CancellationToken.None);
     
        // Prepare client environment
        IsUserLoggedIn = true;
        var userIdDto = await api.Queries.SendAsync(new GetMyUserIdQuery());
        _userId = userIdDto.UserId;
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