using Api.Definitions.Generated;

namespace Api.Definitions.Requests.Queries;

[Request("login", false, RequestType.Query)]
public record LoginQuery(string Email, string Password) : IRequest<LoginResponse>;