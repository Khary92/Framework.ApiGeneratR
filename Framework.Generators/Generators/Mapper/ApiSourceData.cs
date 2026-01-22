using System.Collections.Immutable;

namespace Framework.Generators.Generators.Mapper;

public class ApiSourceData(
    string route,
    bool requiresAuth,
    string httpMethod,
    string requestType,
    string shortName,
    string fullName,
    ImmutableArray<string> members,
    string type)
{
    public string Route { get; } = route;
    public bool RequiresAuth { get; } = requiresAuth;
    public string HttpMethod { get; } = httpMethod;
    public string RequestType { get; } = requestType;
    public string ShortName { get; } = shortName;
    public string FullName { get; } = fullName;
    public ImmutableArray<string> Members { get; } = members;
    public string Type { get; } = type;
}