using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;

namespace Api.Definitions.Events.Message;

[Event("message-received")]
public record MessageReceivedEvent(
    Guid Id,
    Guid ConversationId,
    Guid OriginUserId,
    string Text,
    DateTime TimeStamp);