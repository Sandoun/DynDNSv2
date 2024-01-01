using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynDNSv2;

class Program
{
    static void Main(string[] args) => Task.Run(AsyncMain).Wait();

    static async Task AsyncMain()
    {

        var builder = Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            logging.AddSimpleConsole(settings =>
            {
                settings.SingleLine = true;
            });
        })
        .ConfigureServices(services =>
        {
            services.AddHostedService<Updater>();
        });

        var host = builder.Build();

        await host.RunAsync();

    }

}
