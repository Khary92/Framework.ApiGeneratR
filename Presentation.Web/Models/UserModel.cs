namespace Presentation.Web.Models;

public class UserModel(Guid userId, string loginName, string firstName, string lastName)
{
    public Guid UserId { get; init; } = userId;
    public string LoginName { get; set; } = loginName;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string FullName => $"{FirstName} {LastName}";

    public static UserModel Empty => new(Guid.Empty, string.Empty, string.Empty, string.Empty);

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(LoginName);
    }
}