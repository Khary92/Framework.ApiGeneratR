using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Queries;

[Request("get-users", true, RequestType.Query)]
public record GetAllUsersQuery : RequestResponseTag<List<UserDto>>;