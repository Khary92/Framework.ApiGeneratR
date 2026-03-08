using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Commands;

[Request("change-password", "User", RequestType.Command)]
public record ChangePasswordCommand(string OldPassword, string NewPassword, Guid IdentityId = default)
    : RequestResponseTag<CommandResponse>
{
    public override string ToString() =>
        $"ChangePasswordCommand (OldPassword: {OldPassword}, NewPassword: [redacted], IdentityId: [redacted])";
}