namespace LocalQ.Contracts;

public record QueueStatistics
{
    public int Count { get; set; }
    public long SizeBytes { get; set; }
}