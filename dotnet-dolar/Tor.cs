using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Knapcode.TorSharp;
using Spectre.Console;

namespace Devlooped;

public class Tor : IDisposable
{
    TorSharpProxy? proxy;
    TorSharpSettings settings = new()
    {
        EnableSecurityProtocolsForFetcher = false,
        ZippedToolsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "dolar", "tor", "zip"),
        ExtractedToolsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "dolar", "tor", "bin"),
        WriteToConsole = Debugger.IsAttached,
        PrivoxySettings =
        {
            Disable = true,
        },
        TorSettings =
        {
            SocksPort = 1338,
        },
    };

    public async Task StartAsync() //=> await AnsiConsole.Status().StartAsync("Fetching Tor tools", async ctx =>
    {
        var fetcher = new TorSharpToolFetcher(settings, new HttpClient(new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
        }));

        var updates = await fetcher.CheckForUpdatesAsync();
        await fetcher.FetchAsync(updates);

        proxy = new TorSharpProxy(settings);
        //ctx.Status("Configuring Tor");
        await proxy.ConfigureAsync();
        //ctx.Status("Starting Tor");
        await proxy.StartAsync();
    }//);

    public async Task RestartAsync()// => await AnsiConsole.Status().StartAsync("Restarting Tor", async ctx =>
    {
        if (proxy != null)
        {
            await proxy.GetNewIdentityAsync();
            return;
        }

        proxy?.Stop();
        proxy?.Dispose();

        proxy = new TorSharpProxy(settings);
        //ctx.Status("Configuring Tor");
        await proxy.ConfigureAsync();
        //ctx.Status("Starting Tor");
        await proxy.StartAsync();
    }//);


    public void Dispose()
    {
        proxy?.Stop();
        proxy?.Dispose();
        proxy = null;
    }
}
