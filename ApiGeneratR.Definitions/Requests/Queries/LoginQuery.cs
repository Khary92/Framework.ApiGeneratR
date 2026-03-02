using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Mediator;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("login", false, RequestType.Query)]
public record LoginQuery(string Email, string Password) : IRequest<LoginResponse>;