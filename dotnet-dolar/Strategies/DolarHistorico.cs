using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Devlooped.Web;
using Polly;

namespace Devlooped;

public class DolarHistorico(CultureInfo culture, Policy policy) : IDolarStrategy
{
    public Rate GetRate(DateOnly date)
    {
        var slug = date.ToString("dd-MMMM-yyyy", culture);

        // retry the web request in case of transient errors
        var page = policy.Execute(() => HtmlDocument.Load($"https://dolarhistorico.com/dolar-blue/cotizacion/{slug}"));

        var hrow = page.CssSelectElement("#content > .container .row .row");
        Debug.Assert(hrow != null);

        var values = hrow.CssSelectElements(".h5").ToList();
        Debug.Assert(values.Count > 2);

        var hbuy = values[0].Value;
        var hsell = values[1].Value;

        var buy = double.Parse(hbuy, culture);
        var sell = double.Parse(hsell, culture);

        return new Rate(date, buy, sell);
    }
}
