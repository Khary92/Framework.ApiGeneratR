using ApiGeneratR.Definitions.Generated;

namespace ApiGeneratR.Definitions.Events.User;

[Event("user-updated")]
public record UserUpdatedEvent(
    Guid Id,
    string LoginName,
    string FirstName,
    string LastName);