using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KafkaDemo.Functions;

public class KafkaFunctions
{
    private readonly ILogger<KafkaFunctions> _log;

    public KafkaFunctions(ILogger<KafkaFunctions> log) => _log = log;

    // Batch trigger (recommended for throughput)
    [Function("KafkaBatch")]
    public void KafkaBatch(
        [KafkaTrigger(
            brokerList: "%KafkaBrokerList%",
            topic: "%KafkaTopic%",
            ConsumerGroup = "%KafkaConsumerGroup%",
            IsBatched = true
        )] IReadOnlyCollection<string> events)
    {
        _log.LogWarning($"*** {nameof(KafkaBatch).ToUpper()} ***");

        foreach (var e in events)
        {
            _log.LogInformation("KafkaBatch message: {Body}", e);
        }
    }

    // Single-message trigger (simpler)
    [Function("KafkaSingle")]
    public void KafkaSingle(
        [KafkaTrigger(
            brokerList: "%KafkaBrokerList%",
            topic: "%KafkaTopic2%",
            ConsumerGroup = "%KafkaConsumerGroup%"
        )] string body)
    {
        _log.LogWarning($"*** {nameof(KafkaSingle).ToUpper()} ***");

        //_log.LogInformation("KafkaSingle message: {Body}", body);

        var kafkaMessage = JsonSerializer.Deserialize<KafkaMessage>(body);

        var innerMsgJson = kafkaMessage.Value
                                           .Replace("\\u0022", "\"")    // convert \u0022 → "
                                           .Replace("\\\\", "\\");      // handle double escaping if needed

        var order = JsonSerializer.Deserialize<OrderValue>(innerMsgJson);

        Console.WriteLine($"Offset: {kafkaMessage.Offset} | Partition: {kafkaMessage.Partition} | Key: {kafkaMessage.Key} | OrderId: {order.OrderId}, Status: {order.Status}");
    }
}