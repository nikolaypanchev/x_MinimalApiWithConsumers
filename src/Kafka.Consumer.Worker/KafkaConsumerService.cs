using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class KafkaConsumerService : BackgroundService
{
    private readonly ConsumerConfig _config;
    private readonly ILogger<KafkaConsumerService> _log;
    private readonly string _topic;

    public KafkaConsumerService(ConsumerConfig config, ILogger<KafkaConsumerService> log, string topic)
        => (_config, _log, _topic) = (config, log, topic);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Kafka consumer starting. topic={Topic} group={Group}", _topic, _config.GroupId);

        return Task.Run(() =>
        {
            using var consumer = new ConsumerBuilder<string, string>(_config).Build();
            consumer.Subscribe(_topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = consumer.Consume(TimeSpan.FromMilliseconds(250));
                    if (cr is null) continue;
                    _log.LogInformation("Kafka received: {Offset} key={Key} value={Value}", cr.TopicPartitionOffset, cr.Message.Key, cr.Message.Value);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                consumer.Close();
                _log.LogInformation("Kafka consumer stopping.");
            }
        }, stoppingToken);
    }
}
