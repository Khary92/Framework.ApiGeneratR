using Framework.Contract.Attributes;
using Framework.Contract.Mediator;
using Framework.Example.Entities;

namespace Framework.Example.Queries;

[ApiDefinition("/get-users", false)]
public class GetAllUsersQuery : IRequest<List<User>>;
