using Api.Definitions.Dto;
using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;

namespace Api.Definitions.Events.User;

[Event("user-deleted")]
public record UserDeletedEvent(UserDto User);