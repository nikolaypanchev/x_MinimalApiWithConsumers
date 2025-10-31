using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class RabbitConsumerService : BackgroundService
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitConsumerService> _log;
    private readonly string _queue;
    private readonly ushort _prefetch;
    private readonly bool _autoAck;

    public RabbitConsumerService(ConnectionFactory factory, ILogger<RabbitConsumerService> log, string queue, ushort prefetch, bool autoAck)
        => (_factory, _log, _queue, _prefetch, _autoAck) = (factory, log, queue, prefetch, autoAck);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Rabbit consumer starting. queue={Queue} prefetch={Prefetch} autoAck={AutoAck}", _queue, _prefetch, _autoAck);

        using var conn = _factory.CreateConnection();
        using var ch = conn.CreateModel();
        ch.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        ch.BasicQos(0, _prefetch, false);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            var text = Encoding.UTF8.GetString(ea.Body.ToArray());
            _log.LogInformation("Rabbit received: {Text}", text);
            await Task.Delay(50, stoppingToken);
            if (!_autoAck) ch.BasicAck(ea.DeliveryTag, multiple: false);
        };

        ch.BasicConsume(_queue, autoAck: _autoAck, consumer: consumer);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(500, stoppingToken);
        }
        catch (TaskCanceledException) { }
        _log.LogInformation("Rabbit consumer stopping.");
    }
}
