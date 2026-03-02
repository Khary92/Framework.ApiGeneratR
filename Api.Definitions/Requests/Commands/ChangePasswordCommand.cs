using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Mediator.Contract;

namespace Api.Definitions.Requests.Commands;

[Request("change-password", true, RequestType.Command)]
public record ChangePasswordCommand(string OldPassword, string NewPassword, Guid IdentityId = default)
    : IRequest<CommandResponse>;