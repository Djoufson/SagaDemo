using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SagaDemo.PaymentService.Configurations;
using SagaDemo.PaymentService.Events;

namespace SagaDemo.PaymentService.Services.Orchestrator;

public class OrchestratorClient : IOrchestratorClient
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<OrchestratorClient> _logger;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;

    public OrchestratorClient(
        RabbitMqSettings settings,
        ILogger<OrchestratorClient> logger)
    {
        _settings = settings;
        _logger = logger;
        var factory = new ConnectionFactory()
        {
            HostName = _settings.Host,
            Port = _settings.Port
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection?.CreateModel();
            _channel?.ExchangeDeclare("orchestrator", ExchangeType.Direct);
            if (_connection is not null)
                _connection.ConnectionShutdown += (_, __) => _logger.LogCritical("RabbitMQ connection shut down");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to connect to rabbitMQ");
        }
    }

    public Task RaisePaymentSucceededEvent(PaymentSucceeded @event)
    {
        string message = JsonSerializer.Serialize(@event);
        RaiseEvent(message);
        return Task.CompletedTask;
    }

    public Task RaisePaymentFailedEvent(PaymentFailed @event)
    {
        string message = JsonSerializer.Serialize(@event);
        RaiseEvent(message);
        return Task.CompletedTask;
    }

    private void RaiseEvent(string message)
    {
        if (_connection?.IsOpen ?? false)
            SendMessage(message);
        else
            _logger.LogInformation("The connection is closed", message);
    }

    #region Setup methods
    private void SendMessage(string message)
    {
        byte[] body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish("orchestrator", "paymentService", null, body);
    }

    public void Dispose()
    {
        if (_channel?.IsOpen ?? false)
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
    #endregion
}
