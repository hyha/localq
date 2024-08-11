namespace LocalQ.Models;

public record MessageEnvelope
{
    public required string Id { get; set; }
    public Dictionary<string, string>? Payload { get; set; }
}