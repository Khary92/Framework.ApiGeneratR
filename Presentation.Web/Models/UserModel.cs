namespace Presentation.Web.Models;

public class UserModel(Guid userId, Guid conversationId, string loginName, string firstName, string lastName)
{
    public Guid UserId { get; init; } = userId;
    public Guid ConversationId { get; init; } = conversationId;
    public string LoginName { get; set; } = loginName;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string FullName => $"{FirstName} {LastName}";

    public static UserModel Empty => new(Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty);

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(LoginName);
    }
}