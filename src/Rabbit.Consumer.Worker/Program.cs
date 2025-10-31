using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Config
        string host = Env("RABBIT_HOST", "localhost");
        int port = int.TryParse(Env("RABBIT_PORT", "5672"), out var p) ? p : 5672;
        string user = Env("RABBIT_USER", "guest");
        string pass = Env("RABBIT_PASS", "guest");
        string vhost = Env("RABBIT_VHOST", "/");

        string queue = "demo-queue";
        ushort prefetch = 20;
        bool autoAck = false;

        // simple CLI parse
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--queue": queue = args[++i]; break;
                case "--prefetch": prefetch = ushort.Parse(args[++i]); break;
                case "--auto-ack": autoAck = true; break;
            }
        }

        builder.Services.AddSingleton(new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            VirtualHost = vhost,
            DispatchConsumersAsync = true
        });

        builder.Services.AddHostedService(sp =>
            new RabbitConsumerService(sp.GetRequiredService<ConnectionFactory>(), sp.GetRequiredService<ILogger<RabbitConsumerService>>(), queue, prefetch, autoAck));

        var app = builder.Build();
        await app.RunAsync();

        static string Env(string key, string fallback) => Environment.GetEnvironmentVariable(key) ?? fallback;
    }
}
