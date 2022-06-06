using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using Devlooped.Xml.Css;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("http")
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

builder.Services.AddSingleton((_) => Playwright.CreateAsync().Result);
builder.Services.AddSingleton((services) => services.GetRequiredService<IPlaywright>().Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    ExecutablePath = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "chrome"),
    Headless = true,
}).Result);

var app = builder.Build();

app.UseRouting();

app.MapGet("/", async (IHttpClientFactory http) =>
{
    var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
    if (status != HttpStatusCode.OK)
        return Results.StatusCode((int)status);

    return Results.Ok(quotes!);
});

app.MapGet("deps", () =>
{
    // List chromium dependencies to see if they are satisfied.
    var proc = Process.Start(new ProcessStartInfo("ldd", "./chrome")
    {
        RedirectStandardOutput = true,
        WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native")
    });

    proc?.WaitForExit();
    return proc?.StandardOutput.ReadToEnd();
});

app.MapGet("/clarius", async ([FromServices] IBrowser browser) =>
{
    // Showcase how to navigate a page using chromium and get its HTML
    var page = await browser.NewPageAsync();
    await page.GotoAsync("https://clarius.org", new PageGotoOptions
    {
        Timeout = 0,
        WaitUntil = WaitUntilState.NetworkIdle
    });

    return Results.Content("<html>" + await page.InnerHTMLAsync("body") + "</html>", "text/html; charset=UTF-8");
});

app.MapGet("/{id}", async (string id, IHttpClientFactory http) =>
{
    var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
    if (status != HttpStatusCode.OK)
        return Results.StatusCode((int)status);

    if (!quotes!.ContainsKey(id))
        return Results.NotFound();

    return Results.Ok(quotes[id].Price);
});

app.Run();

async Task<(HttpStatusCode, Dictionary<string, Quote>?)> GetQuotesAsync(HttpClient http)
{
    var response = await http.GetAsync("https://www.infobae.com/economia/");

    if (!response.IsSuccessStatusCode)
        return (response.StatusCode, default);

    response.EnsureSuccessStatusCode();
    var doc = await response.Content.ReadAsDocumentAsync();
    var exchange = doc.CssSelectElement(".excbar");
    if (exchange == null)
        return (HttpStatusCode.NotFound, default);

    var quotes = exchange.CssSelectElements("a")
        .Select(x => new
        {
            title = x.CssSelectElement("p.exc-tit")?.Value.Replace("\"", ""),
            value = double.Parse(x.CssSelectElement("p.exc-val")?.Value ?? "0", NumberStyles.AllowDecimalPoint),
            url = x.CssSelectElement("p.exc-tit")?.Value switch
            {
                string s when s.Contains("Bco") => "/oficial",
                string s when s.Contains("Libre") => "/blue",
                string s when s.Contains("Solidario") => "/tarjeta",
                string s when s.Contains("liqui") => "/ccl",
                string s when s.Contains("MEP") => "/mep",
                _ => "/"
            }
        })
        .Where(x => x.url != "/" && x.title != null)
        .Select(x => new Quote(x.url.Substring(1), x.title!, x.value))
        .ToDictionary(x => x.Id);

    return (HttpStatusCode.OK, quotes);
}

record Quote([property: JsonIgnore] string Id, string Title, double Price)
{
    public string Url = "/" + Id;
}