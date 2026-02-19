using System.Collections.Immutable;

namespace Shared.Contract.Generator.Mapper;

public class EventSourceData(
    string @namespace,
    string typeName,
    string fullTypeName,
    string eventType,
    ImmutableArray<FieldData> properties)
{
    public string Namespace { get; } = @namespace;
    public string TypeName { get; } = typeName;
    public string FullTypeName { get; } = fullTypeName;
    public string EventType { get; } = eventType;
    public ImmutableArray<FieldData> Properties { get; } = properties;
}