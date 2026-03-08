namespace Api.Definitions.Dto;

public record CommandResponse(bool IsSuccessful, string Message = "");