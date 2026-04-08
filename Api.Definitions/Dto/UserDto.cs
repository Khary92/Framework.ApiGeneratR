using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DataTransferObject]
public record UserDto(
    Guid Id,
    string LoginName,
    string FirstName,
    string LastName);