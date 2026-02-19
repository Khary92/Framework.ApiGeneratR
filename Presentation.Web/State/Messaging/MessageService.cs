using System.Collections.Concurrent;
using Api.Definitions.Events.Message;
using Api.Definitions.Generated;
using Api.Definitions.Requests.Queries;
using Presentation.Web.Mapper;
using Presentation.Web.Models;
using Shared.Contracts.EventBus;

namespace Presentation.Web.State.Messaging;

public class MessageService : IMessageService
{
    private readonly List<IDisposable> _disposables = [];
    private readonly ConcurrentDictionary<Guid, List<MessageModel>> _messagesDict = new();
    private readonly QuerySender _querySender;

    public MessageService(QuerySender querySender, IEventSubscriber eventSubscriber)
    {
        _querySender = querySender;

        var newMessageSub = eventSubscriber.Subscribe<MessageReceivedEvent>(@event =>
        {
            var messageModel = @event.ToMessageModel();
            var list = _messagesDict.GetOrAdd(messageModel.ConversationId, _ => []);
            list.Add(messageModel);
            OnMessageReceived?.Invoke();
            return Task.CompletedTask;
        });
        _disposables.Add(newMessageSub);
    }

    public event Action? OnMessageReceived;

    public async Task<List<MessageModel>> GetMessagesForSelectedUser(UserModel selectedUser)
    {
        if (!_messagesDict.ContainsKey(selectedUser.ConversationId))
        {
            var messageModels =
                await _querySender.SendAsync(new GetMessagesForConversationIdQuery(selectedUser.ConversationId));
            _messagesDict.TryAdd(selectedUser.ConversationId, messageModels.Select(m => m.ToMessageModel()).ToList());
        }

        return !_messagesDict.TryGetValue(selectedUser.ConversationId, out var value)
            ? []
            : value;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables) disposable.Dispose();
    }
}