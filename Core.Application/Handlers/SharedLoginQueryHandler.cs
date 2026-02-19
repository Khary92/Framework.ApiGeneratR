using Api.Definitions.Requests.Queries;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class SharedLoginQueryHandler(IUnitOfWork db, IAuthService authService)
    : IRequestHandler<SharedLoginQuery, SharedLoginResponse>
{
    public Task<SharedLoginResponse> HandleAsync(SharedLoginQuery query, CancellationToken ct = default)
    {
        var user = db.Users.FirstOrDefault(u => u.LoginName == query.Email);

        if (user == null)
            return Task.FromResult(new SharedLoginResponse(false, string.Empty));

        var token = authService.GetToken(user.IdentityId, query.Email, query.Password, user.Role);

        return Task.FromResult(string.IsNullOrEmpty(token)
            ? new SharedLoginResponse(false, string.Empty)
            : new SharedLoginResponse(true, token));
    }
}