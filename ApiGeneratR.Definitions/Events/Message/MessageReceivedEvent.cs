using ApiGeneratR.Definitions.Generated;

namespace ApiGeneratR.Definitions.Events.Message;

[Event("message-received")]
public record MessageReceivedEvent(
    Guid Id,
    string ConversationId,
    Guid OriginUserId,
    string Text,
    DateTime TimeStamp);