namespace Framework.Contract.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class WebsocketEventAttribute(string eventType) : Attribute
{
    public string EventType { get; } = eventType;
    public string? Description { get; init; }
}