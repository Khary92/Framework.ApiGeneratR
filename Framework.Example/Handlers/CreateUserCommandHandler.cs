using Framework.Contract.Mediator;
using Framework.Contract.Repository;
using Framework.Example.Commands;
using Framework.Example.Entities;

namespace Framework.Example.Handlers;

public class CreateUserCommandHandler(IRepository<User> userRepository)
    : IRequestHandler<CreateUserCommand, bool>
{
    public async Task<bool> HandleAsync(CreateUserCommand request)
    {
        await userRepository.AddAsync(new User(Guid.NewGuid(), request.Name));
        return true;
    }
}