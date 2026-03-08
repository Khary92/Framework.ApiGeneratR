namespace Api.Definitions.Requests.Queries;

public record LoginResponse(bool IsLoginSuccessful, string Token)
{
    public override string ToString() => $"LoginResponse (IsLoginSuccessful: {IsLoginSuccessful}, Token: [redacted])";
}