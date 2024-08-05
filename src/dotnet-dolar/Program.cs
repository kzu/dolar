using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Devlooped;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp<DolarCommand>();

// Alias -? to -h for help
if (args.Contains("-?"))
{
    args = args.Select(x => x == "-?" ? "-h" : x).ToArray();
}

if (args.Contains("--debug"))
{
    Debugger.Launch();
    args = args.Where(args => args != "--debug").ToArray();
}

app.Configure(config =>
{
    config.SetApplicationName(ThisAssembly.Project.ToolCommandName);
    config.SetApplicationVersion(ThisAssembly.Project.Version);

    if (Environment.GetEnvironmentVariables().Contains("NO_COLOR") &&
        config.Settings.HelpProviderStyles?.Options is { } options)
    {
        options.DefaultValue = Style.Plain;
    }
});

if (args.Contains("--version"))
{
    AnsiConsole.MarkupLine($"{ThisAssembly.Project.ToolCommandName} version [lime]{ThisAssembly.Project.Version}[/] ({ThisAssembly.Project.BuildDate})");
    AnsiConsole.MarkupLine($"[link]{ThisAssembly.Git.Url}/releases/tag/{ThisAssembly.Project.BuildRef}[/]");
    if (await CheckUpdates(args) is string message)
        AnsiConsole.MarkupLine(message);

    return 0;
}

if (Environment.GetEnvironmentVariable("HELP") == "true")
    return app.Run(args);

var updates = Task.Run(() => CheckUpdates(args));
var exit = app.Run(args);

if (await updates is string update)
{
    AnsiConsole.MarkupLine(update);

    if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
        AnsiConsole.Confirm("Actualizar automáticamente?") == true)
    {
        ScheduleUpdate();
    }
    else
    {
        var localVersion = new NuGetVersion(ThisAssembly.Project.Version);
        AnsiConsole.MarkupLine($"Actualizar con: [yellow]dotnet[/] tool update -g {ThisAssembly.Project.PackageId}" +
            (localVersion.IsPrerelease || localVersion.Major == 42 ? " --add-source https://pkg.kzu.app/index.json --prerelease" : ""));
    }
}

return exit;

static async Task<string?> CheckUpdates(string[] args)
{
    if (args.Contains("-u") && !args.Contains("--unattended"))
        return default;

    var providers = Repository.Provider.GetCoreV3();
    var localVersion = new NuGetVersion(ThisAssembly.Project.Version);
    var repository = new SourceRepository(new PackageSource(
        localVersion.IsPrerelease || localVersion.Major == 42 ?
        "https://pkg.kzu.app/index.json" :
        "https://api.nuget.org/v3/index.json"), providers);

    var resource = await repository.GetResourceAsync<PackageMetadataResource>();
    var metadata = await resource.GetMetadataAsync(ThisAssembly.Project.PackageId, true, false,
        new SourceCacheContext
        {
            NoCache = true,
            RefreshMemoryCache = true,
        },
        NuGet.Common.NullLogger.Instance, CancellationToken.None);

    var update = metadata
        .Select(x => x.Identity)
        .Where(x => x.Version > localVersion)
        .OrderByDescending(x => x.Version)
        .Select(x => x.Version)
        .FirstOrDefault();

    if (update != null)
    {
        return $"Hay una nueva version de [yellow]{ThisAssembly.Project.PackageId}[/]: [dim]v{localVersion.ToNormalizedString()}[/] -> [lime]v{update.ToNormalizedString()}[/]";
    }

    return default;
}

static void ScheduleUpdate()
{
    var pid = Process.GetCurrentProcess().Id;
    var command = $@"Wait-Process -Id {pid} -Timeout {20} -ErrorAction SilentlyContinue; dotnet tool update --global {ThisAssembly.Project.PackageId}";
    var localVersion = new NuGetVersion(ThisAssembly.Project.Version);
    if (localVersion.IsPrerelease || localVersion.Major == 42)
        command += " --add-source https://pkg.kzu.app/index.json --prerelease";

    Process.Start(new ProcessStartInfo
    {
        FileName = "powershell.exe",
        Arguments = $"-NoProfile -NoLogo -NonInteractive -ExecutionPolicy unrestricted -command {command}",
        UseShellExecute = false
    });
}