using System;
using System.Threading.Tasks;
using Framework.Contract.Mediator;
using Framework.Contract.Repository;
using Framework.Example.Commands;
using Framework.Example.Entities;

namespace Framework.Example.Handlers;

public class CreateUserCommandHandler(IRepository<User> userRepository)
    : IRequestHandler<CreateUserCommand, CommandResponse>
{
    public async Task<CommandResponse> HandleAsync(CreateUserCommand request)
    {
        await userRepository.AddAsync(new User(Guid.NewGuid(), request.Name));
        return new CommandResponse(true);
    }
}