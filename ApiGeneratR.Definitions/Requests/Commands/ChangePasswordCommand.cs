using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Tags;

namespace ApiGeneratR.Definitions.Requests.Commands;

[Request("change-password", true, RequestType.Command)]
public record ChangePasswordCommand(string OldPassword, string NewPassword, Guid IdentityId = default)
    : RequestResponseTag<CommandResponse>
{
    public override string ToString() =>
        $"ChangePasswordCommand (OldPassword: {OldPassword}, NewPassword: [redacted], IdentityId: [redacted])";
}