using Framework.Contract.Attributes;

namespace Framework.Example.Entities;

[DomainEntity(PersistenceType.InMemory)]
public class User(Guid id, string name)
{
    public Guid Id { get; } = id;
    public string Name { get; set; } = name;
    
    public static User Default => new(Guid.NewGuid(), "Default User");
}