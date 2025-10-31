using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static void Main(string[] args)
    {
        var host = new HostBuilder()
                                .ConfigureFunctionsWorkerDefaults()
                                .ConfigureLogging(b => b.AddConsole())
                                .Build();

        host.Run();
    }
}