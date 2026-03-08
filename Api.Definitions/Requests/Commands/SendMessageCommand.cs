using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Commands;

[Request("send-message", "User", RequestType.Command)]
public record SendMessageCommand(string Message, Guid TargetUserId, Guid IdentityId = default)
    : RequestResponseTag<CommandResponse>
{
    public override string ToString() =>
        $"SendMessageCommand (Message: {Message}, TargetUserId: {TargetUserId}, IdentityId: [redacted])";
}