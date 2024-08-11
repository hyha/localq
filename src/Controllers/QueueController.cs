using System.Net.Mime;

using LocalQ.Components;
using LocalQ.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace LocalQ.Controllers;

[ApiController]
[Route("queue")]
public class QueueController : ControllerBase
{
    private readonly ILogger<QueueController> _logger;
    private readonly IPersistentQueue _persistentQueue;

    public QueueController(ILogger<QueueController> logger, IPersistentQueue persistentQueue)
    {
        _logger = logger;
        _persistentQueue = persistentQueue;
    }

    [HttpGet()]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<string> PeekQueue([FromQuery] bool dequeue = false)
    {
        if (dequeue)
        {
            if (_persistentQueue.TryDequeue(out var dequeueItem))
            {
                return Ok(dequeueItem);
            }
            return NoContent();
        }

        if (_persistentQueue.TryPeek(out var peekItem))
        {
            return Ok(peekItem);
        }
        return NoContent();
    }

    [HttpPost()]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult Enqueue([FromBody] Dictionary<string, string> value)
    {
        _persistentQueue.Enqueue(value);
        return Created();
    }

    [HttpGet("stats")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<QueueStatistics> GetStats()
    {
        var count = _persistentQueue.GetCount();
        var size = _persistentQueue.GetDiskSizeBytes();
        return new QueueStatistics
        {
            Count = count,
            SizeBytes = size,
        };
    }
}
