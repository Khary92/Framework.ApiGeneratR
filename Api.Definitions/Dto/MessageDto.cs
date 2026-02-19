namespace Api.Definitions.Dto;

public class MessageDto(
    Guid id,
    string conversationId,
    Guid originUserId,
    string text,
    DateTime timeStamp,
    bool isAnswered = false)
{
    public Guid Id => id;
    public bool IsAnswered { get; set; } = isAnswered;
    public string Text { get; set; } = text;
    public Guid OriginUserId { get; set; } = originUserId;
    public DateTime TimeStamp { get; set; } = timeStamp;
    public string ConversationId { get; set; } = conversationId;
}