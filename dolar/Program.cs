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

//var app = builder.Build();

//app.UseRouting();

//app.MapGet("/", async (IHttpClientFactory http, HttpContext context, ILogger<Program> logger) =>
//{
//    var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
//    if (status != HttpStatusCode.OK)
//    {
//        logger.LogWarning("Could not fetch webpage from Infobae. Status: {status}", status);
//        return Results.StatusCode((int)status);
//    }

//    if (!(quotes?.Count > 0))
//    {
//        logger.LogWarning("No quotes found in Infobae webpage.");
//        return Results.NotFound();
//    }

//    var id = context.Request.Query.Keys.FirstOrDefault(x => x != "badge");
//    var quote = default(Quote);

//    if (id != null && !quotes.TryGetValue(id, out quote))
//        return Results.NotFound("Invalid dolar kind. Must be one of: oficial/blue/tarjeta/mep/ccl.");

//    if (context.Request.Query.Keys.Contains("badge"))
//    {
//        // Can only return badge for a single value
//        if (id == null)
//            return Results.BadRequest("Need to specify oficial/blue/tarjeta/mep/ccl.");

//        return Results.Ok(new
//        {
//            schemaVersion = 1,
//            label = quote!.Title,
//            message = "$ " + quote.Price
//        });
//    }
//    else if (id != null)
//    {
//        return Results.Ok(quote!.Price);
//    }

//    return Results.Ok(quotes);
//});

//app.MapGet("/{id}", async (string id, IHttpClientFactory http) =>
//{
//    var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
//    if (status != HttpStatusCode.OK)
//        return Results.StatusCode((int)status);

//    if (!quotes!.TryGetValue(id, out var quote))
//        return Results.NotFound("Invalid dolar kind. Must be one of: oficial/blue/tarjeta/mep/ccl.");

//    return Results.Ok(quote.Price);
//});

//app.Run();
