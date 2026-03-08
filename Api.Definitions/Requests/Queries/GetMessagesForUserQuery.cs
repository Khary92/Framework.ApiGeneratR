using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Queries;

[Request("get-messages-for-Id", true, RequestType.Query)]
public record GetMessagesForUserQuery(Guid UserId, Guid IdentityId = default) : RequestResponseTag<MessagesWrapper>
{
    public override string ToString() => $"GetMessagesForUserQuery (UserId: {UserId}, IdentityId: [redacted])";
}