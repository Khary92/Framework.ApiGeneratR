using System.Collections.Immutable;

namespace Shared.Contract.Generator.Mapper;

public class RequestData(
    string route,
    bool requiresAuth,
    bool requestHasIdentityId,
    string httpMethod,
    string cqsType,
    string requestShortName,
    string requestFullName,
    string returnValueFullName,
    ImmutableArray<string> members,
    string dataStructureType)
{
    public string Route { get; } = route;
    public bool RequiresAuth { get; } = requiresAuth;
    public bool RequestHasIdentityId { get; } = requestHasIdentityId;
    public string HttpMethod { get; } = httpMethod;
    public string CqsType { get; } = cqsType;
    public string RequestShortName { get; } = requestShortName;
    public string RequestFullName { get; } = requestFullName;
    public string ReturnValueFullName { get; } = returnValueFullName;
    public ImmutableArray<string> Members { get; } = members;
    public string DataStructureType { get; } = dataStructureType;
}