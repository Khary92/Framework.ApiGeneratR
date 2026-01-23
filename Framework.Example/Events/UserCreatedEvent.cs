using Framework.Contract.Attributes;

namespace Framework.Example.Events;

[WebsocketEvent("user-created", Description = "Fired when a new user is created")]
public record UserCreatedEvent(
    Guid Id,
    string Name
);