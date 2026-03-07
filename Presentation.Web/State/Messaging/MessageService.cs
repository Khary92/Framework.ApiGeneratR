using System.Collections.Concurrent;
using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Events.Message;
using ApiGeneratR.Definitions.Events.User;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Requests.Queries;
using Presentation.Web.Mapper;
using Presentation.Web.Models;
using Presentation.Web.State.Login;

namespace Presentation.Web.State.Messaging;

[ApiConsumer([typeof(MessageReceivedEvent)])]
public partial class MessageService : IMessageService
{
    private readonly ILoginService _loginService;
    private readonly ConcurrentDictionary<string, List<MessageModel>> _messagesDict = new();

    public MessageService(IApiContainer apiContainer, ILoginService loginService) : this(apiContainer)
    {
        _loginService = loginService;
    }

    public event Action? OnMessageReceived;

    private Task HandleMessageReceivedEventAsync(MessageReceivedEvent @event)
    {
        var isCurrentUser = _loginService.IsCurrentUser(@event.OriginUserId);
        var messageModel = @event.ToMessageModel(isCurrentUser);

        var list = _messagesDict.GetOrAdd(@event.ConversationId, _ => []);
        list.Add(messageModel);

        OnMessageReceived?.Invoke();
        return Task.CompletedTask;
    }
    
    public async Task<List<MessageModel>> GetMessagesForSelectedUser(UserModel selectedUser)
    {
        var messageWrapper = await Queries.SendAsync(new GetMessagesForUserQuery(selectedUser.UserId));

        if (_messagesDict.TryGetValue(messageWrapper.ConversationId, out var cached))
            return cached;

        var messageModels = messageWrapper.Messages.Select(m => m.ToMessageModel()).ToList();
        _messagesDict[messageWrapper.ConversationId] = messageModels;
        return messageModels;
    }
}