using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Mediator;

namespace ApiGeneratR.Definitions.Requests.Commands;

[Request("change-password", true, RequestType.Command)]
public record ChangePasswordCommand(string OldPassword, string NewPassword, Guid IdentityId = default)
    : IRequest<CommandResponse>
{
    public override string ToString() => $"ChangePasswordCommand (OldPassword: {OldPassword}, NewPassword: [redacted], IdentityId: [redacted])";   
}