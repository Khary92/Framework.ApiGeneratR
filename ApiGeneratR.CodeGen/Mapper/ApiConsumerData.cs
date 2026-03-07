using System.Collections.Immutable;

namespace ApiGeneratR.CodeGen.Mapper;

public record ApiConsumerData(
    ImmutableArray<TypeNames> TypeNames,
    string ConsumerNamespace,
    string ConsumerClassName);

public record TypeNames(string EventShortName, string EventLongName);