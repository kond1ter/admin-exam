using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MessageSender.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IModel _channel;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IModel channel, ILogger<MessageController> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    [HttpPost("send")]
    public IActionResult SendMessage([FromBody] MessageDto message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "",
                routingKey: "messages",
                basicProperties: null,
                body: body);

            _logger.LogInformation("Message sent: {Message}", message.Text);
            return Ok(new { success = true, message = "Message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class MessageDto
{
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

