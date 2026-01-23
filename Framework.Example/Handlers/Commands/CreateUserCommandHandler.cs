using Framework.Contract.Attributes;
using Framework.Contract.Mediator;
using Framework.Contract.Repository;
using Framework.Example.Entities;
using Framework.Example.Events;
using Framework.Generated;
using Framework.Reusables.Websocket;

namespace Framework.Example.Handlers.Commands;

[ApiDefinition("/create-user", false)]
public record CreateUserCommand(string Name) : IRequest<bool>;

public class CreateUserCommandHandler(IRepository<User> userRepository, WebsocketRegistry wsRegistry)
    : IRequestHandler<CreateUserCommand, bool>
{
    public async Task<bool> HandleAsync(CreateUserCommand request)
    {
        var user = new User(Guid.NewGuid(), request.Name);
        await userRepository.AddAsync(user);

        var createdEvent = new UserCreatedEvent(user.Id, user.Name);
        await wsRegistry.BroadcastAsync(createdEvent.ToSocketMessage());
        return true;
    }
}