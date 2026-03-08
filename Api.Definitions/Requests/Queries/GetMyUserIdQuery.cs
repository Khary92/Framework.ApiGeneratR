using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Queries;

[Request("get-own-user-Id", true, RequestType.Query)]
public record GetMyUserIdQuery(Guid IdentityId = default) : RequestResponseTag<UserIdDto>
{
    public override string ToString() => $"GetMyUserIdQuery (IdentityId: [redacted])";
}