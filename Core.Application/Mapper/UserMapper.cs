using Api.Definitions.Dto;
using Api.Definitions.Requests.Commands;
using Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Core.Application.Mapper;

[Mapper]
public partial class UserMapper
{
    public User ToDomainEntity(CreateUserCommand command)
    {
        return new User(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), command.LoginName,
            command.FirstName, command.LastName);
    }

    public UserDto ToAdminDto(User user)
    {
        return new UserDto(user.Id,
            user.ConversationId,
            user.LoginName,
            user.FirstName,
            user.LastName);
    }
}