using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory storage for messages
var messages = new List<MessageDto>();
var messagesLock = new object();

// Register message storage service before building
builder.Services.AddSingleton<List<MessageDto>>(messages);
builder.Services.AddSingleton<object>(messagesLock);

// Configure RabbitMQ connection
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
var rabbitPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

var factory = new ConnectionFactory
{
    HostName = rabbitHost,
    Port = rabbitPort,
    UserName = rabbitUser,
    Password = rabbitPass
};

// Start RabbitMQ consumer in background
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
channel.QueueDeclare(queue: "messages", durable: false, exclusive: false, autoDelete: false, arguments: null);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var messageJson = Encoding.UTF8.GetString(body);
    var message = JsonSerializer.Deserialize<MessageDto>(messageJson);
    
    if (message != null)
    {
        lock (messagesLock)
        {
            messages.Add(message);
        }
        Console.WriteLine($"Received message: {message.Text}");
    }
};

channel.BasicConsume(queue: "messages", autoAck: true, consumer: consumer);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpMetrics();
app.MapMetrics();

app.MapControllers();

app.Run();

public class MessageDto
{
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

