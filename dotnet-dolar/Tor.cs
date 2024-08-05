using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Knapcode.TorSharp;

namespace Devlooped;

public class Tor(IProgress<string>? progress = null) : IDisposable
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

    public async Task StartAsync()
    {
        var fetcher = new TorSharpToolFetcher(settings, new HttpClient(new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
        }));

        progress?.Report("Actualizando herramientas de anonimización");
        var updates = await fetcher.CheckForUpdatesAsync();
        await fetcher.FetchAsync(updates);

        proxy = new TorSharpProxy(settings);
        await proxy.ConfigureAsync();

        progress?.Report("Iniciando anonimización");
        await proxy.StartAsync();
    }

    public async Task RestartAsync()
    {
        if (proxy != null)
        {
            await proxy.GetNewIdentityAsync();
            return;
        }

        progress?.Report("Iniciando anonimización");

        proxy = new TorSharpProxy(settings);
        await proxy.ConfigureAsync();
        await proxy.StartAsync();
    }

    public void Dispose()
    {
        proxy?.Stop();
        proxy?.Dispose();
        proxy = null;
    }
}
