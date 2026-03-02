using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Api.Definitions.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("get-users", true, RequestType.Query)]
public record GetAllUsersQuery : IRequest<List<UserDto>>;