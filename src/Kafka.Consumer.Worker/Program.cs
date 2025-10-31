using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        string bootstrap = Env("KAFKA_BOOTSTRAP", "localhost:9092");
        string topic = "demo-topic";
        string group = "demo-group";

        // simple CLI parse
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--topic": topic = args[++i]; break;
                case "--group": group = args[++i]; break;
                case "--bootstrap": bootstrap = args[++i]; break;
            }
        }

        builder.Services.AddSingleton(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = group,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        });

        builder.Services.AddHostedService(sp =>
            new KafkaConsumerService(sp.GetRequiredService<ConsumerConfig>(), sp.GetRequiredService<ILogger<KafkaConsumerService>>(), topic));

        var app = builder.Build();
        await app.RunAsync();

        static string Env(string key, string fallback) => Environment.GetEnvironmentVariable(key) ?? fallback;
    }
}
