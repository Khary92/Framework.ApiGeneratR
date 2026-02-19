namespace Api.Definitions.Dto;

public class UserDto(
    Guid id,
    Guid conversationId,
    string loginName,
    string firstName,
    string lastName)
{
    public Guid Id => id;
    public Guid ConversationId => conversationId;
    public string LoginName { get; set; } = loginName;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
}