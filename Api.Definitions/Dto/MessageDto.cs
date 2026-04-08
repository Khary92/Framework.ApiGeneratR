using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DTO]
public record MessageDto(
    Guid Id,
    string ConversationId,
    Guid OriginUserId,
    string Text,
    DateTime TimeStamp,
    bool IsAnswered = false);