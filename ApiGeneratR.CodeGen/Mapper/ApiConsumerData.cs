using System.Collections.Immutable;

namespace ApiGeneratR.CodeGen.Mapper;

public record ApiConsumerData(
    ImmutableArray<string> GlobalEventTypesNameSpaces,
    string ConsumerNamespace,
    string ConsumerClassName);