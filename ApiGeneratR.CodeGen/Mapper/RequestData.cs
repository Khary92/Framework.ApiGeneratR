using System.Collections.Immutable;

namespace ApiGeneratR.CodeGen.Mapper;

public record RequestData(
    string Route,
    bool RequiresAuth,
    bool RequestHasIdentityId,
    string HttpMethod,
    string CqsType,
    string RequestShortName,
    string RequestFullName,
    string ReturnValueFullName,
    ImmutableArray<string> Members,
    string DataStructureType);