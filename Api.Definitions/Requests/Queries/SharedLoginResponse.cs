namespace Api.Definitions.Requests.Queries;

public record SharedLoginResponse(bool IsLoginSuccessful, string Token);