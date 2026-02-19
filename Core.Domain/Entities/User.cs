using static System.String;

namespace Core.Domain.Entities;

public class User(
    Guid id,
    Guid identityId,
    string loginName,
    string firstName,
    string lastName,
    string role = "user")
{
    public Guid Id => id;
    public Guid IdentityId { get; set; } = identityId;
    public string LoginName { get; set; } = loginName;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string Role { get; set; } = role;

    public static User Default => new(Guid.Empty, Guid.Empty, Empty, Empty, Empty, Empty);
}