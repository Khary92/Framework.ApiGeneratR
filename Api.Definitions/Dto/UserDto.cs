namespace Api.Definitions.Dto;

public class UserDto(
    Guid id,
    string loginName,
    string firstName,
    string lastName)
{
    public Guid Id => id;
    public string LoginName { get; set; } = loginName;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
}