using System.Collections.Immutable;

namespace ApiGeneratR.Mapper;

public record ApiEnumData(string Name, ImmutableArray<string> Fields);