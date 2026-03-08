using Api.Definitions.Dto;
using Api.Definitions.Events.User;
using Api.Definitions.Requests.Commands;
using Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Core.Application.Mapper;

[Mapper]
public partial class UserMapper
{
    public User ToDomainEntity(CreateUserCommand command)
    {
        return new User(Guid.NewGuid(), Guid.NewGuid(), command.LoginName, command.FirstName, command.LastName);
    }

    public UserDto ToDto(User user)
    {
        return new UserDto(user.Id,
            user.LoginName,
            user.FirstName,
            user.LastName);
    }

    public UserCreatedEvent ToCreatedEvent(User user)
    {
        return new UserCreatedEvent(user.Id,
            user.LoginName,
            user.FirstName,
            user.LastName);
    }
}