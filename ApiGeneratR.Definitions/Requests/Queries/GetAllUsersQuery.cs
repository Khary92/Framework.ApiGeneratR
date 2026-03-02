using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Mediator;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("get-users", true, RequestType.Query)]
public record GetAllUsersQuery : IRequest<List<UserDto>>;