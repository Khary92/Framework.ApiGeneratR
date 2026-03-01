using System;

namespace Shared.Contract.Generator.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class EventAttribute(string eventType) : Attribute
{
    public string EventType { get; } = eventType;
}