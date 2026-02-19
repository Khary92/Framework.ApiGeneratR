using Api.Definitions.Events.User;
using Api.Definitions.Generated;
using Api.Definitions.Requests.Queries;
using Presentation.Web.Mapper;
using Presentation.Web.Models;
using Shared.Contracts.EventBus;

namespace Presentation.Web.State.User;

public class UserService(
    QuerySender querySender,
    IEventSubscriber eventSubscriber) : IUserService
{
    private readonly List<IDisposable> _subscriptions = [];
    public event Func<Task>? OnCollectionChanged;

    public List<UserModel> Users
    {
        get => field ?? [];
        private set;
    }

    public async Task InitializeAsync()
    {
        var users = await querySender.SendAsync(new GetAllUsersQuery());
        Users = users.Select(u => u.ToUserModel()).ToList();

        // check if already subscribed
        if (_subscriptions.Count != 0) return;

        var createdSub = eventSubscriber.Subscribe<UserCreatedEvent>(async notification =>
        {
            Users.Add(notification.User.ToUserModel());
            if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
        });
        _subscriptions.Add(createdSub);

        var deletedSub = eventSubscriber.Subscribe<UserDeletedEvent>(async notification =>
        {
            var user = Users.FirstOrDefault(user => user.UserId == notification.User.Id);

            if (user == null) return;

            Users.Remove(user);

            if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
        });
        _subscriptions.Add(deletedSub);

        var updatedSub = eventSubscriber.Subscribe<UserUpdatedEvent>(async notification =>
        {
            var user = Users.FirstOrDefault(user => user.UserId == notification.User.Id);

            if (user == null) return;

            Users.Remove(user);
            Users.Add(notification.User.ToUserModel());

            if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
        });
        _subscriptions.Add(updatedSub);

        if (OnCollectionChanged != null) await OnCollectionChanged.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();

        _subscriptions.Clear();

        OnCollectionChanged = null;

        await Task.CompletedTask;
    }
}