using Api.Definitions.Dto;
using Api.Definitions.Events.Message;
using Presentation.Web.Models;

namespace Presentation.Web.Mapper;

public static class ModelExtensions
{
    public static UserModel ToUserModel(this UserDto userDto)
    {
        return new UserModel(userDto.Id, userDto.ConversationId, userDto.LoginName, userDto.FirstName,
            userDto.LastName);
    }

    public static MessageModel ToMessageModel(this MessageDto messageDto)
    {
        return new MessageModel(messageDto.Text, messageDto.TimeStamp, messageDto.OriginUserId,
            messageDto.ConversationId);
    }

    public static MessageModel ToMessageModel(this MessageReceivedEvent messageReceivedEvent)
    {
        return new MessageModel(messageReceivedEvent.Text, DateTime.Now, messageReceivedEvent.OriginUserId,
            messageReceivedEvent.ConversationId);
    }
}