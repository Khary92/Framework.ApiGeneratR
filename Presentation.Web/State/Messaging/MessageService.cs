using System.Collections.Concurrent;
using ApiGeneratR.Definitions.Events.Message;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Requests.Queries;
using Presentation.Web.Mapper;
using Presentation.Web.Models;
using Presentation.Web.State.Login;

namespace Presentation.Web.State.Messaging;

public class MessageService : IMessageService
{
    private readonly List<IDisposable> _disposables = [];
    private readonly ConcurrentDictionary<string, List<MessageModel>> _messagesDict = new();
    private readonly IApiFacade _api; 
    
    public MessageService(IApiFacade api, ILoginService loginService)
    {
        _api = api;

        var newMessageSub = api.EventSubscriber.Subscribe<MessageReceivedEvent>(@event =>
        {
            var isCurrentUser = loginService.IsCurrentUser(@event.OriginUserId);
            var messageModel = @event.ToMessageModel(isCurrentUser);

            var list = _messagesDict.GetOrAdd(@event.ConversationId, _ => []);
            list.Add(messageModel);

            OnMessageReceived?.Invoke();
            return Task.CompletedTask;
        });
        _disposables.Add(newMessageSub);
    }

    public event Action? OnMessageReceived;

    public async Task<List<MessageModel>> GetMessagesForSelectedUser(UserModel selectedUser)
    {
        var messageWrapper = await _api.Queries.SendAsync(new GetMessagesForUserQuery(selectedUser.UserId));

        if (_messagesDict.TryGetValue(messageWrapper.ConversationId, out var cached))
            return cached;

        var messageModels = messageWrapper.Messages.Select(m => m.ToMessageModel()).ToList();
        _messagesDict[messageWrapper.ConversationId] = messageModels;
        return messageModels;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables) disposable.Dispose();
    }
}