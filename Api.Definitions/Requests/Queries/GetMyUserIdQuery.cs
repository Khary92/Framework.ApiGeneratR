using Api.Definitions.Dto;
using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;
using Shared.Contracts.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("get-own-user-id", true, RequestType.Query)]
public record GetMyUserIdQuery(Guid IdentityId = default) : IRequest<UserIdDto>;