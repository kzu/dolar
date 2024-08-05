using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;

namespace Devlooped;

public class RateCache(string provider, DolarType type, DolarOperation operation)
{
    static readonly string baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "dolar");

    readonly string path = Path.Combine(baseDir, $"{provider}-{type.ToString().ToLowerInvariant()}-{operation.ToString().ToLowerInvariant()}.json");

    public async Task<Dictionary<DateOnly, double>> ReadAsync()
    {
        if (File.Exists(path))
        {
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<DateOnly, double>>(await File.ReadAllTextAsync(path));
                if (data == null)
                {
                    File.Delete(path);
                    return new Dictionary<DateOnly, double>();
                }

                return data;
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]No se pudo restaurar el cache de cotizaciones históricas[/]: {e.Message}");
                File.Delete(path);
            }
        }

        return new Dictionary<DateOnly, double>();
    }

    public async void WriteAsync(Dictionary<DateOnly, double> data)
    {
        try
        {
            Directory.CreateDirectory(baseDir);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]No se pudo guardar el cache de cotizaciones históricas[/]: {e.Message}");
        }
    }
}
