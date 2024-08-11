using LocalQ.Models;

namespace LocalQ.Components;

public interface IPersistentQueue
{
    void Enqueue(Dictionary<string, string> item);
    bool TryDequeue(out MessageEnvelope result);
    bool TryPeek(out MessageEnvelope result);
    int GetCount();
    long GetDiskSizeBytes();
}