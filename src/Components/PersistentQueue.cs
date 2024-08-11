using System.Collections.Concurrent;
using System.Text.Json;

using LocalQ.Models;

namespace LocalQ.Components;

public class PersistentQueue : IPersistentQueue
{
    private readonly string _persistencePath;
    private readonly ConcurrentQueue<string> _memoryQueue;
    private readonly SemaphoreSlim _memoryQueueLock;
    private int _sequenceNumber;

    public PersistentQueue(IConfiguration configuration)
    {
        _persistencePath = configuration["QueueName"] ?? throw new ArgumentException("QueueName not configured");
        _memoryQueue = new ConcurrentQueue<string>();
        _memoryQueueLock = new SemaphoreSlim(1, 1);
        Directory.CreateDirectory(_persistencePath);
        RestoreQueueFromPersistence();
        _sequenceNumber = 0;
    }

    public void RestoreQueueFromPersistence()
    {
        _memoryQueueLock.Wait();
        try
        {
            var files = Directory.GetFiles(_persistencePath);
            foreach (var file in files.OrderBy(f => f))
            {
                _memoryQueue.Enqueue(file);
            }
        }
        finally
        {
            _memoryQueueLock.Release();
        }
    }

    public void Enqueue(Dictionary<string, string> item)
    {
        _memoryQueueLock.Wait();
        try
        {
            var id = GenerateId();
            var fileName = Path.Combine(_persistencePath, $"{id}.json");
            File.WriteAllText(fileName, JsonSerializer.Serialize(item));
            _memoryQueue.Enqueue(fileName);
        }
        finally
        {
            _memoryQueueLock.Release();
        }
    }

    public bool TryDequeue(out MessageEnvelope result)
    {
        if (_memoryQueue.TryDequeue(out var fileName))
        {
            _memoryQueueLock.Wait();
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName));
                File.Delete(fileName);
                result = new MessageEnvelope
                {
                    Id = Path.GetFileNameWithoutExtension(fileName),
                    Payload = payload
                };
                if (_memoryQueue.IsEmpty) _sequenceNumber = 0;
                return true;
            }
            finally
            {
                _memoryQueueLock.Release();
            }
        }
        result = default!;
        return false;
    }

    public bool TryPeek(out MessageEnvelope result)
    {
        if (_memoryQueue.TryPeek(out var fileName))
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName));
            result = new MessageEnvelope
            {
                Id = Path.GetFileNameWithoutExtension(fileName),
                Payload = payload
            };
            return true;
        }
        result = default!;
        return false;
    }

    public int GetCount()
    {
        return _memoryQueue.Count;
    }

    public long GetDiskSizeBytes()
    {
        var directoryInfo = new DirectoryInfo(_persistencePath);
        return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }

    private string GenerateId()
    {
        var timestampString = string.Format("{0:x16}", DateTime.UtcNow.Ticks);
        var sequenceString = string.Format("{0:x8}", Interlocked.Increment(ref _sequenceNumber));
        return $"{timestampString}{sequenceString}";
    }

}