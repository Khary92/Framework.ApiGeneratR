namespace Framework.Generators.Generators.Mapper;

public class RepositorySourceData(
    string entityShortName,
    string entityFullName,
    string @namespace,
    string persistenceEnumRepresentation)
{
    public string EntityShortName { get; } = entityShortName;
    public string EntityFullName { get; } = entityFullName;
    public string Namespace { get; } = @namespace;
    private string PersistenceEnumRepresentation { get; } = persistenceEnumRepresentation;

    public string RepositoryShortName()
    {
        switch (PersistenceEnumRepresentation)
        {
            case "0": return $"{EntityShortName}InMemoryRepository";
        }
        
        return "UnknownTypeRepository";
    }
}
