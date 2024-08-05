using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient("http")
            .ConfigurePrimaryHttpMessageHandler(() =>
                new ContentLocationHttpHandler(
                    new XhtmlHttpHandler(new HttpClientHandler
                    {
                        AllowAutoRedirect = true,
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        UseDefaultCredentials = true,
                        UseProxy = false,
                        Proxy = null
                    })));

    })
    .Build();

host.Run();