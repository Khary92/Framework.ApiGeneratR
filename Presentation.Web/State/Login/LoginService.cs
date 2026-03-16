using Api.Definitions.Requests.Queries;
using ApiGeneratR.Attributes;
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

        SetToken(token);
        await EventReceiver.ConnectAsync(SocketUris.WebSocketUri, CancellationToken.None);
        var userIdDto = await Queries.SendAsync(new GetMyUserIdQuery());
        _userId = userIdDto.UserId;
        await EventPublisher.PublishAsync(new UserLoggedInEvent());
    }

    public async Task Logout()
    {
        IsUserLoggedIn = false;

        SetToken(string.Empty);
        _userId = Guid.Empty;
        await EventReceiver.DisposeAsync();
        await EventPublisher.PublishAsync(new UserLoggedOutEvent());
    }
}