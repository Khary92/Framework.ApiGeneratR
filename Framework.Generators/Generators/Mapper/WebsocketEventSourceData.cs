using System.Collections.Immutable;

namespace Framework.Generators.Generators.Mapper;

public class WebsocketEventSourceData(
    string @namespace,
    string typeName,
    string fullTypeName,
    string eventType,
    string? description,
    ImmutableArray<FieldData> properties)
{
    public string Namespace { get; } = @namespace;
    public string TypeName { get; } = typeName;
    public string FullTypeName { get; } = fullTypeName;
    public string EventType { get; } = eventType;
    public string? Description { get; } = description;
    public ImmutableArray<FieldData> Properties { get; } = properties;
}

public class FieldData(
    string name,
    string type)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
}