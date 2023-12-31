using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SagaDemo.EmailService.Configurations;
using SagaDemo.EmailService.Services.EventProcessing;

namespace SagaDemo.EmailService.Services.Jobs;

public class BroadcastSubscriber : BackgroundService
{
    private readonly IEventProcessor _eventProcessor;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _queueName;
    private readonly ILogger<BroadcastSubscriber> _logger;

    public BroadcastSubscriber(
        IEventProcessor eventProcessor,
        RabbitMqSettings rabbitMqSettings,
        ILogger<BroadcastSubscriber> logger)
    {
        _eventProcessor = eventProcessor;
        _settings = rabbitMqSettings;
        _logger = logger;
        InitializeRabbitMq();
    }

    public void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.Host,
                Port = _settings.Port
            };
            _connection = factory.CreateConnection();
            _channel = _connection?.CreateModel();
            _channel?.ExchangeDeclare("broadcast", ExchangeType.Fanout);
            _queueName = _channel?.QueueDeclare().QueueName;

            _channel?.QueueBind(_queueName, "broadcast", "");
            if (_connection is not null)
                _connection.ConnectionShutdown += (_, __) => _logger.LogWarning("RabbitMQ connection shut down");

            _logger.LogInformation("Successfully connected to RabbitMQ");
        }
        catch (Exception e)
        {
            _logger.LogError("--> Unable to connect to RabbitMQ: {Message}", e.Message);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(_channel is null || _connection is null)
        {
            _logger.LogInformation("Attempting to reconnect to RabbitMQ");
            InitializeRabbitMq();
            return Task.CompletedTask;
        }

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += EventReceived;
        _channel.BasicConsume(_queueName, true, consumer);
        return Task.CompletedTask;
    }

    private void EventReceived(object? sender, BasicDeliverEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Body.ToArray());
        _eventProcessor.Process(message);
    }

    public new void Dispose()
    {
        if (_channel?.IsOpen ?? false)
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
