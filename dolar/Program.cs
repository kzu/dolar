using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using Devlooped.Xml.Css;

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

var app = builder.Build();

app.UseRouting();

app.MapGet("/", async (IHttpClientFactory http, HttpContext context) =>
{
    var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
    if (status != HttpStatusCode.OK || quotes == null)
        return Results.StatusCode((int)status);

    var id = context.Request.Query.Keys.FirstOrDefault(x => x != "badge");
    var quote = default(Quote);
    
    if (id != null && !quotes.TryGetValue(id, out quote))
        return Results.NotFound("Invalid dolar kind. Must be one of: oficial/blue/tarjeta/mep/ccl.");

    if (context.Request.Query.Keys.Contains("badge"))
    {
        // Can only return badge for a single value
        if (id == null)
            return Results.BadRequest("Need to specify oficial/blue/tarjeta/mep/ccl.");

        return Results.Ok(new
        {
            schemaVersion = 1,
            label = quote!.Title,
            message = "$ " + quote.Price
        });
    }
    else if (id != null)
    {
        return Results.Ok(quote);
    }

    return Results.Ok(quotes);
});

app.MapGet("/{id}", async (string id, IHttpClientFactory http) =>
{
    var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
    if (status != HttpStatusCode.OK)
        return Results.StatusCode((int)status);

    if (!quotes!.TryGetValue(id, out var quote))
        return Results.NotFound("Invalid dolar kind. Must be one of: oficial/blue/tarjeta/mep/ccl.");

    return Results.Ok(quote.Price);
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