using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Devlooped.Web;
using System.Globalization;
using System.Text.Json.Serialization;

public class Dolar(ILogger<Dolar> logger, IHttpClientFactory http)
{
    [Function("api")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{id:alpha?}")] HttpRequest req, string? id)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");

        var (status, quotes) = await GetQuotesAsync(http.CreateClient("http"));
        if (status != HttpStatusCode.OK)
        {
            logger.LogWarning("Could not fetch webpage from Infobae. Status: {status}", status);
            return new StatusCodeResult((int)status);
        }

        if (!(quotes?.Count > 0))
        {
            logger.LogWarning("No quotes found in Infobae webpage.");
            return new NotFoundResult();
        }

        id ??= req.Query.Keys.FirstOrDefault(x => x != "badge");
        var quote = default(Quote);

        if (id != null && !quotes.TryGetValue(id, out quote))
            return new NotFoundObjectResult("Invalid dolar kind. Must be one of: oficial/blue/tarjeta/mep/ccl.");

        if (req.Query.Keys.Contains("badge"))
        {
            // Can only return badge for a single value
            if (id == null)
                return new BadRequestObjectResult("Need to specify oficial/blue/tarjeta/mep/ccl.");

            return new OkObjectResult(new
            {
                schemaVersion = 1,
                label = quote!.Title,
                message = "$ " + quote.Price
            });
        }
        else if (id != null)
        {
            return new OkObjectResult(quote!.Price);
        }

        return new OkObjectResult(quotes);

    }

    async Task<(HttpStatusCode, Dictionary<string, Quote>?)> GetQuotesAsync(HttpClient http)
    {
        var response = await http.GetAsync("https://www.infobae.com/economia/");
        if (!response.IsSuccessStatusCode)
            return (response.StatusCode, default);

        var doc = await response.Content.ReadAsDocumentAsync();
        var exchange = doc.CssSelectElement(".exchange-dolar-container");
        if (exchange == null)
            return (HttpStatusCode.NotFound, default);

        var quotes = exchange.CssSelectElements(".exchange-dolar-item")
            .Select(x => new
            {
                title = x.CssSelectElement("a p")?.Value,
                value = double.Parse(
                    x.CssSelectElement(".exchange-dolar-amount")?.Value.Trim().TrimStart('$') ?? "0", 
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("es-AR")),
                url = x.CssSelectElement("a p")?.Value switch
                {
                    string s when s.Contains("BANCO", StringComparison.OrdinalIgnoreCase) => "/oficial",
                    string s when s.Contains("LIBRE", StringComparison.OrdinalIgnoreCase) => "/blue",
                    string s when s.Contains("TURISTA", StringComparison.OrdinalIgnoreCase) => "/tarjeta",
                    string s when s.Contains("LIQUI", StringComparison.OrdinalIgnoreCase) => "/ccl",
                    string s when s.Contains("MEP", StringComparison.OrdinalIgnoreCase) => "/mep",
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
}
