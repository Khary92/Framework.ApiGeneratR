using ApiGeneratR.Attributes;

namespace Api.Definitions.Requests.Queries;

[DataTransferObject]
public record LoginResponse(bool IsLoginSuccessful, string Token)
{
    public override string ToString() => $"LoginResponse (IsLoginSuccessful: {IsLoginSuccessful}, Token: [redacted])";
}