namespace Api.Definitions.Dto;

public record MessagesWrapper(string ConversationId, List<MessageDto> Messages);