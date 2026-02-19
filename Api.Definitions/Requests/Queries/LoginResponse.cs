namespace Api.Definitions.Requests.Queries;

public record LoginResponse(bool IsLoginSuccessful, string Token);