using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Devlooped.Web;
using Polly;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped;

partial class DolarCommand : AsyncCommand<DolarCommand.DolarSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DolarSettings settings) => 
        await AnsiConsole.Status().StartAsync($"Abriendo {Path.GetFileName(settings.FilePath)}", async ctx =>
        {
            var progress = new Progress<string>(value => ctx.Status = value);

            using var xls = new XLWorkbook(settings.FilePath);
            var ws = xls.Worksheet(settings.Sheet);
            var strategy = DolarStrategy.Create(settings.Type!.Value, progress);
            var cache = new RateCache(strategy.Id, settings.Type.Value, settings.Operation!.Value);
            var rates = await cache.ReadAsync();

            try
            {
                var row = 1;
                while (true)
                {
                    var cell = ws.Cell(row, settings.DateColumn);
                    if (cell.IsEmpty())
                        break;

                    // Skip first row if we can't parse a date to allow for a header row
                    if (!DateOnly.TryParseExact(cell.GetText(), "dd/MM/yyyy", out var date) && row == 1)
                    {
                        row++;
                        continue;
                    }

                    if (rates.TryGetValue(date, out var saved))
                    {
                        ws.Cell(row, settings.RateColumn).Value = saved;
                        ctx.Status = $"{date:yyyy-MM-dd} => [grey]{saved}[/]";
                        row++;
                        continue;
                    }

                    var rate = strategy.GetRate(date);
                    if (rate == null)
                    {
                        ctx.Status = $"{date:yyyy-MM-dd} => [red]No disponible[/]";
                        row++;
                        continue;
                    }

                    var value = settings.Operation == DolarOperation.Compra ? rate.Buy :
                                settings.Operation == DolarOperation.Venta ? rate.Sell :
                                (rate.Buy + rate.Sell) / 2;

                    rates.Add(date, value);
                    ctx.Status = $"{date:yyyy-MM-dd} => [lime]{value}[/]";
                    ws.Cell(row, settings.RateColumn).Value = value;

                    row++;
                }
            }
            finally
            {
                xls.Save();
                cache.WriteAsync(rates);
            }

            return 0;
        });

    public class DolarSettings : CommandSettings
    {
        [CommandArgument(0, "<Archivo Excel>")]
        public required string FilePath { get; set; }

        [Description("Número de hoja")]
        [CommandOption("-h|--hoja")]
        public required int Sheet { get; set; } = -1;

        [Description("Número de columna de fecha")]
        [CommandOption("-f|--fecha")]
        public required int DateColumn { get; set; } = -1;

        [Description("Número de columna de cotización")]
        [CommandOption("-c|--cotizacion")]
        public required int RateColumn { get; set; } = -1;

        [Description("Tipo de cotización (billete y divisa, del BCRA")]
        [CommandOption("-t|--tipo <Billete|Divisa|Blue|MEP|CCL|Turista>")]
        public DolarType? Type { get; set; }

        [Description("Operación")]
        [CommandOption("-o|--operacion <Compra|Venta|Promedio>")]
        public DolarOperation? Operation { get; set; }

        [Description("Sobreescribir cotización existente")]
        [CommandOption("-s")]
        [DefaultValue(true)]
        public bool Overwrite { get; set; } = true;

        public override ValidationResult Validate()
        {
            if (string.IsNullOrEmpty(FilePath) && 
                Directory.EnumerateFiles(".", "*.xlsx").ToList() is var files && 
                files.Count == 1)
            {
                FilePath = files[0];
            }

            if (!File.Exists(FilePath))
                return ValidationResult.Error($"El archivo '{FilePath}' no existe.");

            try
            {
                using var fs = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite);
            }
            catch (IOException ex)
            {
                return ValidationResult.Error($"Error al abrir el archivo '{FilePath}' para escritura: {ex.Message}");
            }

            // get values from enum and offer a selection prompt
            Type ??= AnsiConsole.Prompt(new SelectionPrompt<DolarType>()
                .Title("Seleccionar tipo de cotización:")
                .AddChoices(Enum.GetValues<DolarType>()));

            // get values from enum and offer a selection prompt too
            Operation ??= AnsiConsole.Prompt(new SelectionPrompt<DolarOperation>()
                .Title("Seleccionar operación:")
                .AddChoices(Enum.GetValues<DolarOperation>()));

            if (Sheet == -1 || DateColumn == -1 || RateColumn == -1)
            {
                // Offer populating
                using var xls = new XLWorkbook(FilePath);
                if (Sheet == -1)
                {
                    // Enumerate sheets, get title/caption and show a prompt selection
                    var sheets = xls.Worksheets.Select((ws, i) => (ws.Name, Index: i + 1)).ToList();
                    if (sheets.Count == 1)
                    {
                        Sheet = 1;
                    }
                    else
                    {
                        var selected = AnsiConsole.Prompt(new SelectionPrompt<(string Name, int Index)>()
                            .Title("Seleccionar hoja:")
                            .AddChoices(sheets));
                        Sheet = selected.Index;
                    }
                }

                var ws = xls.Worksheet(Sheet);

                if (DateColumn == -1)
                {
                    // Assume first row is header, offer to select from header row
                    var headers = ws.Row(1).Cells().Select((c, i) => (Name: c.GetString(), Index: i + 1)).ToList();
                    var selected = AnsiConsole.Prompt(new SelectionPrompt<(string Name, int Index)>()
                        .Title("Seleccionar columna de fecha:")
                        .AddChoices(headers));

                    DateColumn = selected.Index;
                }

                if (RateColumn == -1)
                {
                    // Assume first row is header, offer to select from header row
                    var headers = ws.Row(1).Cells().Select((c, i) => (Name: c.GetString(), Index: i + 1)).ToList();
                    headers.Add(($"{Type} {Operation}", headers.Count + 1));
                    var selected = AnsiConsole.Prompt(new SelectionPrompt<(string Name, int Index)>()
                        .Title("Seleccionar columna de cotizacion:")
                        .AddChoices(headers));

                    // If we selected the last item, it's the new column, add it to the worksheet
                    if (selected.Index == headers.Count - 1)
                    {
                        ws.Cell(1, selected.Index).Value = selected.Name;
                        xls.Save();
                    }

                    RateColumn = selected.Index + 1;
                }
            }

            return base.Validate();
        }
    }
}
