using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DataTransferObject]
public record UserIdDto(Guid UserId);