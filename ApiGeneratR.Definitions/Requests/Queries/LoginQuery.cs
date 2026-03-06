using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace ApiGeneratR.Definitions.Requests.Queries;

[Request("login", false, RequestType.Query)]
public record LoginQuery(string Email, string Password) : RequestResponseTag<LoginResponse>
{
    public override string ToString() => $"LoginQuery (Email: {Email}, Password: [redacted])";
}