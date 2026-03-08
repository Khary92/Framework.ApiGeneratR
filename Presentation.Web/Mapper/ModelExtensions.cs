using Api.Definitions.Dto;
using Api.Definitions.Events.Message;
using Api.Definitions.Events.User;
using Presentation.Web.Models;

namespace Presentation.Web.Mapper;

public static class ModelExtensions
{
    public static UserModel ToUserModel(this UserDto userDto)
    {
        return new UserModel(userDto.Id, userDto.LoginName, userDto.FirstName,
            userDto.LastName);
    }

    public static UserModel ToUserModel(this UserCreatedEvent @event)
    {
        return new UserModel(@event.Id, @event.LoginName, @event.FirstName,
            @event.LastName);
    }
    
    public static UserModel ToUserModel(this UserUpdatedEvent @event)
    {
        return new UserModel(@event.Id, @event.LoginName, @event.FirstName,
            @event.LastName);
    }

    public static MessageModel ToMessageModel(this MessageDto messageDto, bool isOwnMessage = false)
    {
        return new MessageModel(messageDto.ConversationId, messageDto.Text, messageDto.TimeStamp, isOwnMessage);
    }

    public static MessageModel ToMessageModel(this MessageReceivedEvent messageReceivedEvent, bool isOwnMessage = false)
    {
        return new MessageModel(messageReceivedEvent.ConversationId, messageReceivedEvent.Text, DateTime.Now,
            isOwnMessage);
    }
}