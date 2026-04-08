using System.Collections.Immutable;

namespace ApiGeneratR.Mapper;

public record DtoData(
    string Namespace,
    string TypeName,
    string FullTypeName,
    string EventType,
    ImmutableArray<FieldData> Properties);