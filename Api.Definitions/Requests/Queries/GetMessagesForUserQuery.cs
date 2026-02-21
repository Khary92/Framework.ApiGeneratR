using Api.Definitions.Dto;
using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;
using Shared.Contracts.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("get-messages-for-id", true,RequestType.Query)]
public record GetMessagesForUserQuery(Guid UserId, Guid IdentityId = default) : IRequest<MessagesWrapper>;