using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Api.Definitions.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("get-messages-for-Id", true, RequestType.Query)]
public record GetMessagesForUserQuery(Guid UserId, Guid IdentityId = default) :  IRequest<MessagesWrapper>;