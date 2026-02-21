using Shared.Contracts.Attributes;

namespace Api.Definitions.Events.User;

[Event("user-created")]
public record UserCreatedEvent(
    Guid Id,
    string LoginName,
    string FirstName,
    string LastName);