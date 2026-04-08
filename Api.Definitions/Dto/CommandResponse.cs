using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DataTransferObject]
public record CommandResponse(bool IsSuccessful, string Message = "");