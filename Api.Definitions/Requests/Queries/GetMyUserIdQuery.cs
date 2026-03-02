using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Mediator.Contract;

namespace Api.Definitions.Requests.Queries;

[Request("get-own-user-Id", true, RequestType.Query)]
public record GetMyUserIdQuery(Guid IdentityId = default) : IRequest<UserIdDto>;