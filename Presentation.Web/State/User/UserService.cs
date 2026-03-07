using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Events.User;
using ApiGeneratR.Definitions.Requests.Queries;
using Presentation.Web.Mapper;
using Presentation.Web.Models;

namespace Presentation.Web.State.User;

[ApiConsumer(typeof(UserCreatedEvent), typeof(UserDeletedEvent), typeof(UserUpdatedEvent))]
public partial class UserService : IUserService
{
    public event Func<Task>? OnCollectionChanged;

    public List<UserModel> Users
    {
        get => field ?? [];
        private set;
    }

    public async Task InitializeAsync()
    {
        var users = await Queries.SendAsync(new GetAllUsersQuery());
        Users = users.Select(u => u.ToUserModel()).ToList();
        if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
    }

    
    private async Task HandleUserCreatedEventAsync(UserCreatedEvent @event)
    {
        Users.Add(@event.ToUserModel());
        if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
    }

    private async Task HandleUserDeletedEventAsync(UserDeletedEvent @event)
    {
        var user = Users.FirstOrDefault(user => user.UserId == @event.Id);

        if (user == null) return;

        Users.Remove(user);

        if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
    }

    private async Task HandleUserUpdatedEventAsync(UserUpdatedEvent @event)
    {
        var user = Users.FirstOrDefault(user => user.UserId == @event.Id);

        if (user == null) return;

        Users.Remove(user);
        Users.Add(@event.ToUserModel());

        if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
    }
}