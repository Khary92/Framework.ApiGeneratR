using Api.Definitions.Dto;
using Api.Definitions.Generated;

namespace Api.Definitions.Requests.Queries;

[Request("get-messages-for-Id", true, RequestType.Query)]
public record GetMessagesForUserQuery(Guid UserId, Guid IdentityId = default) : IRequest<MessagesWrapper>;