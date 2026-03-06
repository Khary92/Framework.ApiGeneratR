using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Mediator;
using ApiGeneratR.Definitions.Requests.Commands;
using Core.Application.Ports;

namespace Core.Application.Handlers;

[RequestHandler(typeof(ChangePasswordCommand))]
public class ChangePasswordCommandHandler(IAuthService authService) : IChangePasswordCommandHandler
{
    public Task<CommandResponse> HandleAsync(ChangePasswordCommand request,
        CancellationToken cancellationToken = default)
    {
        var isPasswordChanged =
            authService.ChangePassword(request.IdentityId, request.NewPassword, request.OldPassword);

        var result = isPasswordChanged
            ? new CommandResponse(true, "Password changed successfully.")
            : new CommandResponse(false, "Password change failed.");
        return Task.FromResult(result);
    }
}