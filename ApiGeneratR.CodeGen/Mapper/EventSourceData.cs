using System.Collections.Immutable;

namespace ApiGeneratR.CodeGen.Mapper;

public record EventSourceData(
    string Namespace,
    string TypeName,
    string FullTypeName,
    string EventType,
    ImmutableArray<FieldData> Properties);