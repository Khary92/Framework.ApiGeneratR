using Shared.Contracts.Attributes.Enums;
using HttpMethod = Shared.Contracts.Attributes.Enums.HttpMethod;

namespace Shared.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public class RequestAttribute(
    string route,
    bool requiresAuth,
    RequestType requestType,
    HttpMethod method = HttpMethod.Post)
    : Attribute
{
    public string Route { get; } = route;
    public bool RequiresAuth { get; set; } = requiresAuth;
    public HttpMethod Method { get; } = method;
    public RequestType RequestType { get; } = requestType;
}