using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Mediator;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("get-messages-for-Id", true, RequestType.Query)]
public record GetMessagesForUserQuery(Guid UserId, Guid IdentityId = default) : IRequest<MessagesWrapper>
{
    public override string ToString() => $"GetMessagesForUserQuery (UserId: {UserId}, IdentityId: [redacted])"; 
}