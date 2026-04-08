using ApiGeneratR.Attributes;

namespace Api.Definitions.Dto;

[DataTransferObject]
public record MessagesWrapper(string ConversationId, List<MessageDto> Messages);