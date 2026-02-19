namespace Core.Domain.Entities.Messages;

public class Message(
    Guid id,
    Guid conversationId,
    Guid originUserId,
    string text,
    DateTime timeStamp)
{
    public Guid ConversationId = conversationId;
    public Guid Id = id;
    public Guid OriginUserId = originUserId;
    public string Text = text;
    public DateTime TimeStamp = timeStamp;
}