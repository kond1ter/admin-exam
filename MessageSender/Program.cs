using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddSingleton<IConnection>(sp =>
{
    return factory.CreateConnection();
});

builder.Services.AddSingleton<IModel>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();
    channel.QueueDeclare(queue: "messages", durable: false, exclusive: false, autoDelete: false, arguments: null);
    return channel;
});

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

