using KyberDaemon.Core;
using KyberDaemon.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KyberDaemon;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory);

        if (OperatingSystem.IsWindows())
        {
            builder = builder.UseWindowsService(options =>
            {
                options.ServiceName = "KyberDaemon Post-Quantum Service";
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            builder = builder.UseSystemd();
        }

        builder = builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                options.UseUtcTimestamp = true;
            });
        });

        builder = builder.ConfigureServices(services =>
        {
            services.AddSingleton(new DaemonHostArguments(args));
            services.AddSingleton<DaemonLogger>();
            services.AddSingleton<KyberDaemonService>();
            services.AddHostedService<KyberHostedService>();
        });

        var host = builder.Build();
        await host.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
