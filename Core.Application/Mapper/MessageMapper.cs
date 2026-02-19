using Api.Definitions.Dto;
using Api.Definitions.Events.Message;
using Api.Definitions.Requests.Commands;
using Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Core.Application.Mapper;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName)]
public partial class MessageMapper
{
    public MessageDto ToDto(Message message)
    {
        return new MessageDto(message.Id, message.ConversationId, message.OriginUserId,
            message.Text, message.TimeStamp);
    }

    public Message ToDomain(SendMessageCommand command, string conversationId, Guid originUserId)
    {
        return new Message(Guid.NewGuid(), conversationId, originUserId, command.Message, DateTime.Now);
    }

    public MessageReceivedEvent ToMessageReceivedEvent(Message message)
    {
        return new MessageReceivedEvent(message.Id, message.ConversationId, message.OriginUserId,
            message.Text, message.TimeStamp);
    }
}