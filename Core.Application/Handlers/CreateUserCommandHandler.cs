using Api.Definitions.Dto;
using Api.Definitions.Events.User.Generated;
using Api.Definitions.Requests.Commands;
using Core.Application.Mapper;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class CreateUserCommandHandler(
    ISocketConnectionService socket,
    UserMapper mapper,
    IAuthService authService,
    IUnitOfWork db)
    : IRequestHandler<CreateUserCommand, CommandResponse>
{
    public async Task<CommandResponse> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        try
        {
            return await db.ExecuteAsync(async () =>
            {
                var domainUser = mapper.ToDomainEntity(command);

                //TODO well this is unlikely. Validate this as soon as there is a validation possible
                if (db.Users.Any(u => u.IdentityId == domainUser.IdentityId))
                    throw new InvalidOperationException("User already exists.");

                var newIdentityId = Guid.NewGuid();
                var success =
                    authService.CreateIdentityUser(newIdentityId, domainUser.LoginName, command.InitialPassword);

                if (!success) throw new InvalidOperationException("Could not create identity user.");

                domainUser.IdentityId = newIdentityId;
                db.Users.Add(domainUser);

                await socket.BroadcastToAllUsers(mapper.ToCreatedEvent(domainUser).ToWebsocketMessage(), ct);

                return new CommandResponse(true, "User created successfully");
            }, ct);
        }
        catch (Exception ex)
        {
            return new CommandResponse(false, ex.Message);
        }
    }
}