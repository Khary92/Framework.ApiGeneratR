
using System;
using Shared.Contract.Generator.Attributes;

namespace Api.Definitions.Generated;

[AttributeUsage(AttributeTargets.Class)]
public class RequestAttribute(string route, bool requiresAuth, RequestType requestType, HttpMethod method = HttpMethod.Post) : Attribute
{
    public string Route { get; } = route;
    public bool RequiresAuth { get; set; } = requiresAuth;
    public HttpMethod Method { get; } = method;
    public RequestType RequestType { get; } = requestType;
}