using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlackPDF.PrintService;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
        options.ServiceName = "SlackPDF Print Service")
    .ConfigureServices(services =>
    {
        services.AddSingleton<GhostscriptConverter>();
        services.AddSingleton<TrayNotification>();
        services.AddHostedService<PrintJobService>();
    })
    .Build();

host.Run();
