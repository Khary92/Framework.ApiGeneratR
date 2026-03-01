using Api.Definitions.Dto;
using Api.Definitions.Generated;

namespace Api.Definitions.Requests.Queries;

[Request("get-own-user-Id", true, RequestType.Query)]
public record GetMyUserIdQuery(Guid IdentityId = default) : IRequest<UserIdDto>;