namespace Presentation.Web.Models;

public record MessageModel(string ConversationId, string Text, DateTime Timestamp, bool IsOwnMessage);