namespace Framework.Contract.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DomainEntity(PersistenceType persistenceType): Attribute
{
    public PersistenceType PersistenceType { get; } = persistenceType;
}

// This is a little tricky. The index of the enum is the mapping value in generator
public enum PersistenceType { InMemory }