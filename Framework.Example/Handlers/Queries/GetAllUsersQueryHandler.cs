using Framework.Contract.Attributes;
using Framework.Contract.Mediator;
using Framework.Contract.Repository;
using Framework.Example.Entities;

namespace Framework.Example.Handlers.Queries;

[ApiDefinition("/get-users", false)]
public record GetAllUsersQuery : IRequest<List<User>>;

public class GetAllUsersQueryHandler(IRepository<User> userRepo) : IRequestHandler<GetAllUsersQuery, List<User>>
{
    public async Task<List<User>> HandleAsync(GetAllUsersQuery request)
    {
        return await userRepo.GetAllAsync();
    }
}