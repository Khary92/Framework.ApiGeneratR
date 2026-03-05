using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Requests.Queries;
using Presentation.Web.Events;

namespace Presentation.Web.State.Login;

public class LoginService(IApiFacade api) : ILoginService
{
    public bool IsUserLoggedIn { get; private set; }
    private Guid _userId = Guid.Empty;

    public bool IsCurrentUser(Guid userId) => userId == _userId;

    public async Task Login(string token)
    {
        IsUserLoggedIn = true;
        
        api.SetToken(token);
        await api.WebSocket.ConnectAsync(SocketUris.WebSocketUri, CancellationToken.None);
        var userIdDto = await api.Queries.SendAsync(new GetMyUserIdQuery());
        _userId = userIdDto.UserId;
        await api.EventPublisher.PublishAsync(new UserLoggedInEvent());
    }

    public async Task Logout()
    {
        IsUserLoggedIn = false;
        
        api.SetToken(string.Empty);
        _userId = Guid.Empty;
        await api.WebSocket.DisposeAsync();
        await api.EventPublisher.PublishAsync(new UserLoggedOutEvent());
    }
}