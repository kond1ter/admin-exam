using Microsoft.AspNetCore.Mvc;

namespace MessageReceiver.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly List<MessageDto> _messages;
    private readonly object _messagesLock;
    private readonly ILogger<MessageController> _logger;

    public MessageController(List<MessageDto> messages, object messagesLock, ILogger<MessageController> logger)
    {
        _messages = messages;
        _messagesLock = messagesLock;
        _logger = logger;
    }

    [HttpGet("all")]
    public IActionResult GetAllMessages()
    {
        lock (_messagesLock)
        {
            return Ok(_messages);
        }
    }

    [HttpGet("count")]
    public IActionResult GetMessageCount()
    {
        lock (_messagesLock)
        {
            return Ok(new { count = _messages.Count });
        }
    }
}

public class MessageDto
{
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

