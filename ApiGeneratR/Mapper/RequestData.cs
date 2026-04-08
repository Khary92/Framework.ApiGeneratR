using System.Collections.Immutable;

namespace ApiGeneratR.Mapper;

public record RequestData(
    string Route,
    string AuthPolicy,
    bool RequestHasIdentityId,
    string CqsType,
    string RequestShortName,
    string RequestFullName,
    string ReturnValueFullName,
    string DataStructureType,
    ImmutableArray<FieldData> Properties);