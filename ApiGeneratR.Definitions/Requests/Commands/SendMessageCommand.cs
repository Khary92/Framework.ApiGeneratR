using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Tags;

namespace ApiGeneratR.Definitions.Requests.Commands;

[Request("send-message", true, RequestType.Command)]
public record SendMessageCommand(string Message, Guid TargetUserId, Guid IdentityId = default)
    : RequestResponseTag<CommandResponse>
{
    public override string ToString() =>
        $"SendMessageCommand (Message: {Message}, TargetUserId: {TargetUserId}, IdentityId: [redacted])";
}