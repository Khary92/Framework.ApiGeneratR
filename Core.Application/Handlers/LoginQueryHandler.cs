using Api.Definitions.Requests.Queries;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class LoginQueryHandler(IUnitOfWork db, IAuthService authService)
    : IRequestHandler<LoginQuery, LoginResponse>
{
    public Task<LoginResponse> HandleAsync(LoginQuery query, CancellationToken ct = default)
    {
        var user = db.Users.FirstOrDefault(u => u.LoginName == query.Email);

        if (user == null)
            return Task.FromResult(new LoginResponse(false, string.Empty));

        var token = authService.GetToken(user.IdentityId, query.Email, query.Password, user.Role);

        return Task.FromResult(string.IsNullOrEmpty(token)
            ? new LoginResponse(false, string.Empty)
            : new LoginResponse(true, token));
    }
}