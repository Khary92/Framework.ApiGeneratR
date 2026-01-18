namespace Framework.Contract.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DomainEntity(PersistenceType persistenceType): Attribute
{
    public PersistenceType PersistenceType { get; } = persistenceType;
}

public enum PersistenceType { InMemory }