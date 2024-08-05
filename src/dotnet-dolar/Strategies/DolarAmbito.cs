using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using Polly;

namespace Devlooped;

public class DolarAmbito(DolarType type) : IDolarStrategy
{
    static readonly HttpClient client = new();
    static readonly Policy policy = Policy.Handle<Exception>().WaitAndRetry(2, _ => TimeSpan.FromSeconds(1));
    static readonly CultureInfo culture = new("es-AR");

    static DolarAmbito()
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
    }

    public string Id => "ambito";

    public Rate? GetRate(DateOnly date)
    {
        var from = date.ToString("yyyy-MM-dd");
        // Allow for weekends and some bank holidays. This should get us the next closest working day.
        var to = date.AddDays(5).ToString("yyyy-MM-dd");
        var path = type switch
        {
            DolarType.CCL => "dolarrava/cl",
            DolarType.MEP => "dolarrava/mep",
            DolarType.Turista => "dolarturista",
            DolarType.Blue => "dolar/informal",
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        // retry the web request in case of transient errors
        var data = policy.Execute(() =>
        {

            var json = client.GetFromJsonAsync<List<string[]>>($"https://mercados.ambito.com/{path}/historico-general/{from}/{to}").Result;
            if (json == null || json.Count < 2)
                return [];

            return json;
        });

        return data.Skip(1).Where(x => x.Length >= 2)
            .Select(x => new Rate(
                DateOnly.ParseExact(x[0], "dd/MM/yyyy"),
                double.Parse(x[1], culture),
                x.Length == 2 ? double.Parse(x[1], culture) : double.Parse(x[2], culture)))
            .OrderBy(x => x.Date)
            .FirstOrDefault(x => x.Date >= date);
    }
}
