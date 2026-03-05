using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Mediator;
using ApiGeneratR.Definitions.Requests.Commands;
using Core.Application.Mapper;
using Core.Application.Ports;

namespace Core.Application.Handlers;

public class CreateUserCommandHandler(
    IEventSender eventSender,
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

                await eventSender.BroadcastAsync(mapper.ToCreatedEvent(domainUser).ToWebsocketMessage(), ct);

                return new CommandResponse(true, "User created successfully");
            }, ct);
        }
        catch (Exception ex)
        {
            return new CommandResponse(false, ex.Message);
        }
    }
}