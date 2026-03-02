using ApiGeneratR.Attributes;

namespace ApiGeneratR.Definitions.Events.User;

[Event("user-deleted")]
public record UserDeletedEvent(Guid Id);