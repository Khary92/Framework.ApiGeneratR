using Api.Definitions.Dto;
using Api.Definitions.Requests.Commands;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class ChangePasswordCommandHandler(IAuthService authService)
    : IRequestHandler<ChangePasswordCommand, CommandResponse>
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