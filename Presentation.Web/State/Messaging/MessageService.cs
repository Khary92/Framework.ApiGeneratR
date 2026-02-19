using System.Collections.Concurrent;
using Api.Definitions.Events.Message;
using Api.Definitions.Generated;
using Api.Definitions.Requests.Queries;
using Presentation.Web.Mapper;
using Presentation.Web.Models;
using Presentation.Web.State.Login;
using Shared.Contracts.EventBus;

namespace Presentation.Web.State.Messaging;

public class MessageService : IMessageService
{
    private readonly List<IDisposable> _disposables = [];
    private readonly ConcurrentDictionary<Guid, List<MessageModel>> _messagesDict = new();
    private readonly QuerySender _querySender;

    public MessageService(QuerySender querySender, IEventSubscriber eventSubscriber, ILoginService loginService)
    {
        _querySender = querySender;

        var newMessageSub = eventSubscriber.Subscribe<MessageReceivedEvent>(@event =>
        {
            var messageModel = @event.ToMessageModel(loginService.IsCurrentUser(@event.OriginUserId));
            var list = _messagesDict.GetOrAdd(@event.OriginUserId, _ => []);
            list.Add(messageModel);
            OnMessageReceived?.Invoke();
            return Task.CompletedTask;
        });
        _disposables.Add(newMessageSub);
    }

    public event Action? OnMessageReceived;

    public async Task<List<MessageModel>> GetMessagesForSelectedUser(UserModel selectedUser)
    {
        var messages = await _querySender.SendAsync(new GetMessagesForUserQuery(selectedUser.UserId));
        return messages.Select(m => m.ToMessageModel()).ToList();
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables) disposable.Dispose();
    }
}