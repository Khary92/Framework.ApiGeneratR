using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Tags;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("get-messages-for-Id", true, RequestType.Query)]
public record GetMessagesForUserQuery(Guid UserId, Guid IdentityId = default) : RequestResponseTag<MessagesWrapper>
{
    public override string ToString() => $"GetMessagesForUserQuery (UserId: {UserId}, IdentityId: [redacted])";
}