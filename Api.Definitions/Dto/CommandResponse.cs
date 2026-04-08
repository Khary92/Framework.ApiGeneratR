using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DTO]
public record CommandResponse(bool IsSuccessful, string Message = "");