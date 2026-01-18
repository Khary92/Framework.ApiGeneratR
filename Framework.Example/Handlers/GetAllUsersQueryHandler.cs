using Framework.Contract.Mediator;
using Framework.Contract.Repository;
using Framework.Example.Entities;
using Framework.Example.Queries;

namespace Framework.Example.Handlers;

public class GetAllUsersQueryHandler(IRepository<User> userRepo) : IRequestHandler<GetAllUsersQuery, List<User>>
{
    public async Task<List<User>> HandleAsync(GetAllUsersQuery request)
    {
        return await userRepo.GetAllAsync();
    }
}