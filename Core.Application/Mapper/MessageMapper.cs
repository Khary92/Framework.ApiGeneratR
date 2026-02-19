using Api.Definitions.Dto;
using Api.Definitions.Events.Message;
using Api.Definitions.Requests.Commands;
using Core.Domain.Entities.Messages;
using Riok.Mapperly.Abstractions;

namespace Core.Application.Mapper;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName)]
public partial class MessageMapper
{
    public MessageDto ToAdminDto(Message message)
    {
        return new MessageDto(message.Id, message.ConversationId, message.OriginUserId,
            message.Text, message.TimeStamp);
    }

    public Message ToDomain(SendMessageToUserCommand command, Guid conversationId)
    {
        return new Message(Guid.NewGuid(), conversationId, command.OriginUserId, command.Message, DateTime.Now);
    }

    public MessageReceivedEvent ToPublicMessageReceivedEvent(Message message)
    {
        return new MessageReceivedEvent(message.Id, message.ConversationId, message.OriginUserId,
            message.Text, message.TimeStamp);
    }
}