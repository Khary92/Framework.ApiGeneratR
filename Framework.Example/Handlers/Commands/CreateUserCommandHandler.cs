using Framework.Contract.Attributes;
using Framework.Contract.Mediator;
using Framework.Contract.Repository;
using Framework.Example.Entities;

namespace Framework.Example.Handlers.Commands;

[ApiDefinition("/create-user", false)]
public record CreateUserCommand(string Name) : IRequest<bool>;

public class CreateUserCommandHandler(IRepository<User> userRepository)
    : IRequestHandler<CreateUserCommand, bool>
{
    public async Task<bool> HandleAsync(CreateUserCommand request)
    {
        await userRepository.AddAsync(new User(Guid.NewGuid(), request.Name));
        return true;
    }
}