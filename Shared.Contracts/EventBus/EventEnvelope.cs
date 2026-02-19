namespace Shared.Contracts.EventBus;

public class EventEnvelope(string type, string payload, DateTime timestamp)
{
    public string Type { get; set; } = type;
    public string Payload { get; set; } = payload;
    public DateTime Timestamp { get; set; } = timestamp;
}