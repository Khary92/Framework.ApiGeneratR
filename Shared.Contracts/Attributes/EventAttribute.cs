namespace Shared.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class EventAttribute(string eventType) : Attribute
{
    public string EventType { get; } = eventType;
}