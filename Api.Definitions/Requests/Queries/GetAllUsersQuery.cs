using Api.Definitions.Dto;
using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;
using Shared.Contracts.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("get-users", true, RequestType.Query)]
public record GetAllUsersQuery : IRequest<List<UserDto>>;