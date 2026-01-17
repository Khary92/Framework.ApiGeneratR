namespace Framework.Contract.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ApiDefinition(string route, bool requiresAuth, HttpMethod method = HttpMethod.Post) : Attribute
{
    public string Route { get; } = route;
    public HttpMethod Method { get; } = method;
    public bool RequiresAuth { get; set; } = true;
}

public enum HttpMethod { Get, Post, Put, Delete, Patch }