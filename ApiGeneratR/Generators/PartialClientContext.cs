using System.Collections.Immutable;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Generators;

public record PartialClientContext(
    ImmutableArray<ApiConsumerData> ConsumerData,
    string? NameSpace,
    GlobalOptions Options);