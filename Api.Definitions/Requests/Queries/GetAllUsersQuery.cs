using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Mediator.Contract;

namespace Api.Definitions.Requests.Queries;

[Request("get-users", true, RequestType.Query)]
public record GetAllUsersQuery : IRequest<List<UserDto>>;