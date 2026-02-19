using Shared.Contracts.Attributes;

namespace Api.Definitions.Events.Message;

[Event("message-received")]
public record MessageReceivedEvent(
    Guid Id,
    string ConversationId,
    Guid OriginUserId,
    string Text,
    DateTime TimeStamp);