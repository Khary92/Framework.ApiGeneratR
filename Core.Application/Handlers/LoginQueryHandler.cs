using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Requests.Queries;
using Core.Application.Ports;

namespace Core.Application.Handlers;

[RequestHandler(typeof(LoginQuery))]
public class LoginQueryHandler(IUnitOfWork db, IAuthService authService)
    : ILoginQueryHandler
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