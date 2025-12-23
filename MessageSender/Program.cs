using Prometheus;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON options for record type deserialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Configure RabbitMQ
var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
var factory = new ConnectionFactory
{
    HostName = rabbitConfig["Host"] ?? "rabbitmq",
    Port = int.Parse(rabbitConfig["Port"] ?? "5672"),
    UserName = rabbitConfig["Username"] ?? "guest",
    Password = rabbitConfig["Password"] ?? "guest"
};

builder.Services.AddSingleton<IConnection>(_ => factory.CreateConnection());
builder.Services.AddSingleton<IModel>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();
    channel.QueueDeclare(queue: "messages", durable: false, exclusive: false, autoDelete: false);
    return channel;
});

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
app.MapPost("/api/message/send", (MessageDto message, IModel channel, ILogger<Program> logger) =>
{
    try
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: "messages",
            basicProperties: null,
            body: body);

        logger.LogInformation("Message sent: {Text} at {Timestamp}", message.Text, message.Timestamp);
        
        return Results.Ok(new { success = true, message = "Message sent successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sending message: {Text}", message.Text);
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
})
.WithName("SendMessage")
.WithOpenApi()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

// Map metrics endpoint - must be last
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
