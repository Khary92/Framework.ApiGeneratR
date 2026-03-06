using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Requests.Queries;
using Presentation.Web.Events;

namespace Presentation.Web.State.Login;

[ApiConsumer]
public partial class LoginService : ILoginService
{
    public bool IsUserLoggedIn { get; private set; }
    private Guid _userId = Guid.Empty;

    public bool IsCurrentUser(Guid userId) => userId == _userId;

    public async Task Login(string token)
    {
        IsUserLoggedIn = true;

        Api.SetToken(token);
        await Api.WebSocket.ConnectAsync(SocketUris.WebSocketUri, CancellationToken.None);
        var userIdDto = await Api.Queries.SendAsync(new GetMyUserIdQuery());
        _userId = userIdDto.UserId;
        await Api.EventPublisher.PublishAsync(new UserLoggedInEvent());
    }

    public async Task Logout()
    {
        IsUserLoggedIn = false;

        Api.SetToken(string.Empty);
        _userId = Guid.Empty;
        await Api.WebSocket.DisposeAsync();
        await Api.EventPublisher.PublishAsync(new UserLoggedOutEvent());
    }
}