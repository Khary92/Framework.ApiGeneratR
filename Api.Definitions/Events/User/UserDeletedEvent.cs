using Shared.Contracts.Attributes;

namespace Api.Definitions.Events.User;

[Event("user-deleted")]
public record UserDeletedEvent(Guid Id);