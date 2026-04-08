using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DTO]
public record UserDto(
    Guid Id,
    string LoginName,
    string FirstName,
    string LastName);