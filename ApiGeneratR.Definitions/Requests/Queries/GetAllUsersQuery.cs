using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Tags;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("get-users", true, RequestType.Query)]
public record GetAllUsersQuery : RequestResponseTag<List<UserDto>>;