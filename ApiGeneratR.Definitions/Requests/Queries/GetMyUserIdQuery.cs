using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Mediator;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("get-own-user-Id", true, RequestType.Query)]
public record GetMyUserIdQuery(Guid IdentityId = default) : IRequest<UserIdDto>;