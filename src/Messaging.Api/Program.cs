using Confluent.Kafka;
using Confluent.Kafka.Admin;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Headers = Confluent.Kafka.Headers;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Env-based config
        var rabbitHost = Env("RABBIT_HOST", "localhost");
        var rabbitPort = int.TryParse(Env("RABBIT_PORT", "5672"), out var rp) ? rp : 5672;
        var rabbitUser = Env("RABBIT_USER", "guest");
        var rabbitPass = Env("RABBIT_PASS", "guest");
        var rabbitVHost = Env("RABBIT_VHOST", "/");
        var kafkaBootstrap = Env("KAFKA_BOOTSTRAP", "localhost:9092");

        builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            HostName = rabbitHost,
            Port = rabbitPort,
            UserName = rabbitUser,
            Password = rabbitPass,
            VirtualHost = rabbitVHost
        });

        builder.Services.AddSingleton(sp =>
        {
            var cfg = new ProducerConfig
            {
                BootstrapServers = kafkaBootstrap,
                Acks = Acks.All,
                EnableIdempotence = true,
                LingerMs = 5,
                BatchSize = 32_768
            };
            return new ProducerBuilder<string, string>(cfg).Build();
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.MapPost("/rabbit/publish", (IConnectionFactory factory, string? queue, int? count, int? delayMs, Body body) =>
        {
            queue ??= "demo-queue";
            int n = count.GetValueOrDefault(1);
            int delay = delayMs.GetValueOrDefault(0);

            using var conn = factory.CreateConnection();
            using var ch = conn.CreateModel();
            ch.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var props = ch.CreateBasicProperties();
            props.Persistent = true;

            for (int i = 1; i <= n; i++)
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new { id = i, message = body.Message ?? $"Rabbit message #{i}", createdUtc = DateTime.UtcNow });
                var bytes = Encoding.UTF8.GetBytes(payload);
                ch.BasicPublish("", queue, props, bytes);
                if (delay > 0) Thread.Sleep(delay);
            }

            return Results.Ok(new { sent = n, queue });
        })
        .WithOpenApi();

        app.MapPost("/kafka/publish", async (IProducer<string, string> producer, string? topic, int? count, int? delayMs, Body body) =>
        {
            topic ??= "demo-topic";
            int n = count.GetValueOrDefault(1);
            int delay = delayMs.GetValueOrDefault(0);

            for (int i = 1; i <= n; i++)
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new { id = i, message = body.Message ?? $"Kafka event #{i}", createdUtc = DateTime.UtcNow });
                var dr = await producer.ProduceAsync(topic, new Message<string, string> { Key = i.ToString(), Value = payload });
                if (delay > 0) await Task.Delay(delay);
            }

            // Confluent.Kafka 2.5.x sync flush
            producer.Flush(TimeSpan.FromSeconds(5));

            return Results.Ok(new { sent = n, topic });
        })
        .WithOpenApi();

        app.MapPost("/kafka/publishAzure", async (IProducer<string, string> producer, string? topic, int? count, int? delayMs, Body body) =>
        {
            int n = count.GetValueOrDefault(1);
            topic ??= n == 1 ? "ordersSingle" : "ordersBatch";

            int delay = delayMs.GetValueOrDefault(0);

            for (int i = 1; i <= n; i++)
            {
                var payload = JsonSerializer.Serialize(new { id = i, message = body.Message ?? $"Kafka event #{i}", createdUtc = DateTime.UtcNow });
                var dr = await producer.ProduceAsync(topic, new Message<string, string> { Key = i.ToString(), Value = payload });
                if (delay > 0) await Task.Delay(delay);
            }

            // Confluent.Kafka 2.5.x sync flush
            producer.Flush(TimeSpan.FromSeconds(5));

            return Results.Ok(new { sent = n, topic });
        })
        .WithOpenApi();

        app.Run("http://localhost:5088");

        static string Env(string key, string fallback) => Environment.GetEnvironmentVariable(key) ?? fallback;
    }
}

public record Body(string? Message);

internal class KafkaSingleProducer
{
    internal static async Task Execute()
    {
        const string topicName = "ordersSingle";
        const int numPartitions = 3;
        const short replicationFactor = 1;  // for local setups; use 3+ in production

        var config = new ProducerConfig
        {
            BootstrapServers = "127.0.0.1:9092",
            Acks = Acks.All,
            EnableIdempotence = true,
            ClientId = "orders-producer"
        };

        // ?? Step 1: Ensure the topic exists (create if not)
        await EnsureTopicExistsAsync(config.BootstrapServers, topicName, numPartitions, replicationFactor);

        // ?? Step 2: Produce messages to different partitions
        using var producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => Console.WriteLine($"? Error: {e.Reason}"))
            .Build();

        for (int i = 1; i <= 5; i++)
        {
            var key = $"order-{i}";
            //var value = $"{{\"orderId\":{i},\"status\":\"Created\"}}";

            Status msgStatus = i <= 3 ? Status.Created : Status.Declined;
            Message message1 = new Message(i, msgStatus);
            string msgJson = JsonSerializer.Serialize(message1);

            var partition = new Partition(i % numPartitions);  // distribute messages across partitions

            var headers = new Headers
            {
                { "source", Encoding.UTF8.GetBytes("order-service") },
                { "event-type", Encoding.UTF8.GetBytes("order-created") },
                { "trace-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) }
            };

            var message = new Message<string, string>
            {
                Key = key,
                Value = msgJson,
                Headers = headers
            };

            var result = await producer.ProduceAsync(
                new TopicPartition(topicName, partition),
                message
            );

            Console.WriteLine($"? Sent {msgJson} ? {result.TopicPartitionOffset}");
        }

        producer.Flush(TimeSpan.FromSeconds(5));
        Console.WriteLine("? All messages sent!");
    }

    // Helper: Create topic if missing
    private static async Task EnsureTopicExistsAsync(string bootstrapServers, string topicName, int numPartitions, short replicationFactor)
    {
        var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            bool exists = metadata.Topics.Exists(t => t.Topic == topicName && !t.Error.IsError);

            if (exists)
            {
                Console.WriteLine($"?? Topic '{topicName}' already exists.");
                return;
            }

            Console.WriteLine($"??? Creating topic '{topicName}' with {numPartitions} partitions...");
            await adminClient.CreateTopicsAsync(new TopicSpecification[]
            {
                new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = numPartitions,
                    ReplicationFactor = replicationFactor
                }
            });

            Console.WriteLine($"? Topic '{topicName}' created.");
        }
        catch (CreateTopicsException e)
        {
            if (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
                Console.WriteLine($"?? Topic '{topicName}' already exists.");
            else
                Console.WriteLine($"? Failed to create topic '{topicName}': {e.Results[0].Error.Reason}");
        }
    }
}

public record Message(int orderId, Status status);

public enum Status
{
    Created,
    Declined
}