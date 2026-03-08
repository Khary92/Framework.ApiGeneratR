using ApiGeneratR.Attributes;

namespace Api.Definitions.Events.User;

[Event("user-deleted")]
public record UserDeletedEvent(Guid Id);