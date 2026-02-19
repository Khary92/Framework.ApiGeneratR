using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;
using Shared.Contracts.Mediator;

namespace Api.Definitions.Requests.Queries;

[Request("login", false, RequestType.Query)]
public record LoginQuery(string Email, string Password) : IRequest<LoginResponse>;