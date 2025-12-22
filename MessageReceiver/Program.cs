using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thread-safe message storage
var messages = new ConcurrentBag<MessageDto>();
builder.Services.AddSingleton<ConcurrentBag<MessageDto>>(messages);

// Configure RabbitMQ
var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
var factory = new ConnectionFactory
{
    HostName = rabbitConfig["Host"] ?? "rabbitmq",
    Port = int.Parse(rabbitConfig["Port"] ?? "5672"),
    UserName = rabbitConfig["Username"] ?? "guest",
    Password = rabbitConfig["Password"] ?? "guest"
};

// Register RabbitMQ consumer as hosted service
builder.Services.AddSingleton<IConnection>(_ => factory.CreateConnection());
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpMetrics();

// Minimal API endpoints
app.MapGet("/api/message/all", (ConcurrentBag<MessageDto> messages) =>
{
    return Results.Ok(messages.ToArray());
})
.WithName("GetAllMessages")
.WithOpenApi()
.Produces<MessageDto[]>(StatusCodes.Status200OK);

app.MapGet("/api/message/count", (ConcurrentBag<MessageDto> messages) =>
{
    return Results.Ok(new { count = messages.Count });
})
.WithName("GetMessageCount")
.WithOpenApi()
.Produces<int>(StatusCodes.Status200OK);

// Map metrics endpoint after all other routes
app.MapMetrics();

app.Run();

// DTOs
public record MessageDto
{
    public string Text { get; init; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }
    
    [JsonConstructor]
    public MessageDto(string text, DateTime timestamp)
    {
        Text = text;
        Timestamp = timestamp;
    }
    
    public MessageDto(string text) : this(text, DateTime.UtcNow) { }
}

// RabbitMQ Consumer Service
public class RabbitMqConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ConcurrentBag<MessageDto> _messages;
    private readonly ILogger<RabbitMqConsumerService> _logger;

    public RabbitMqConsumerService(
        IConnection connection,
        ConcurrentBag<MessageDto> messages,
        ILogger<RabbitMqConsumerService> logger)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _messages = messages;
        _logger = logger;
        
        _channel.QueueDeclare(queue: "messages", durable: false, exclusive: false, autoDelete: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<MessageDto>(messageJson);
                
                if (message is not null)
                {
                    _messages.Add(message);
                    _logger.LogInformation("Received message: {Text} at {Timestamp}", message.Text, message.Timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        };

        _channel.BasicConsume(queue: "messages", autoAck: true, consumer: consumer);
        _logger.LogInformation("RabbitMQ consumer started");

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
