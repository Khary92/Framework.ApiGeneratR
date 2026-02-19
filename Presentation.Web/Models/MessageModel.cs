namespace Presentation.Web.Models;

public record MessageModel(string Text, DateTime Timestamp, string UserName, Guid originUserId);