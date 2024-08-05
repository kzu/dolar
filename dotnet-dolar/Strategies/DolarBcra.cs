using System;
using System.Globalization;
using System.Linq;
using Devlooped.Web;
using Polly;

namespace Devlooped;

public class DolarBcra(bool divisa = true) : IDolarStrategy
{
    static readonly Policy policy = Policy.Handle<Exception>().WaitAndRetryForever(_ => TimeSpan.FromSeconds(1));
    static readonly CultureInfo culture = new("es-AR");

    public Rate? GetRate(DateOnly date)
    {
        var slug = date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var id = divisa ? "monedas" : "billetes";
        var url = $"https://www.bna.com.ar/Cotizador/HistoricoPrincipales?id={id}&fecha={slug}&filtroEuro=0&filtroDolar=1";

        // retry the web request in case of transient errors
        var page = policy.Execute(() => HtmlDocument.Load(url));
        var rows = page.CssSelectElements("#tablaDolar tbody tr").ToList();

        if (rows.Count == 0)
            return null;

        var row = rows[0];
        if (rows.Count > 1)
        {
            // find the row with the exact same date requested
            // last column contains the date in the format dd/MM/yyyy
            row = rows.FirstOrDefault(r => 
                DateOnly.TryParseExact(r.CssSelectElement("td:last-child")?.Value, "d/M/yyyy", out var rowDate) &&
                rowDate == date);

            if (row == null)
                return null;
        }

        var values = row.CssSelectElements("td").ToList();

        var hbuy = values[1].Value;
        var hsell = values[2].Value;

        // detect decimal separator since (WTAF?) it's not normalized

        // WTAF? billetes == es-AR format, divisas == en-US ?!
        var buy = double.Parse(hbuy, hbuy.Reverse().First(c => !char.IsNumber(c)) == '.' ? CultureInfo.InvariantCulture : culture);
        var sell = double.Parse(hsell, hsell.Reverse().First(c => !char.IsNumber(c)) == '.' ? CultureInfo.InvariantCulture : culture);

        return new Rate(date, buy, sell);
    }
}
