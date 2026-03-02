using Api.Definitions.Generated;
using Api.Definitions.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("login", false, RequestType.Query)]
public record LoginQuery(string Email, string Password) : IRequest<LoginResponse>;