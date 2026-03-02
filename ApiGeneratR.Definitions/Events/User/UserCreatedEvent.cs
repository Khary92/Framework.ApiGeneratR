using ApiGeneratR.Attributes;

namespace ApiGeneratR.Definitions.Events.User;

[Event("user-created")]
public record UserCreatedEvent(
    Guid Id,
    string LoginName,
    string FirstName,
    string LastName);