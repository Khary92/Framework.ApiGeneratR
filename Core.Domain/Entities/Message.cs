namespace Core.Domain.Entities;

public class Message(
    Guid id,
    string conversationId,
    Guid originUserId,
    string text,
    DateTime timeStamp)
{
    public readonly string ConversationId = conversationId;
    public Guid Id = id;
    public Guid OriginUserId = originUserId;
    public readonly string Text = text;
    public DateTime TimeStamp = timeStamp;
}