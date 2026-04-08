using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DTO]
public record MessagesWrapper(string ConversationId, List<MessageDto> Messages);