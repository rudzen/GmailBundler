using GmailBundler.Configuration;
using GmailBundler.Csv.Interfaces;
using GmailBundler.Downloader.Interfaces;
using GmailBundler.Dto;
using Google.Apis.Gmail.v1;
using Microsoft.Extensions.Options;
using Serilog;

namespace GmailBundler.Downloader;

public sealed class GmailServiceWrapper : IGmailServiceWrapper
{
    public static readonly string[] Scopes = { GmailService.Scope.GmailReadonly };

    private readonly ILogger _logger;
    private readonly ICsvConverter _csvConverter;
    private readonly IGmailDownloader _gmailDownloader;

    private readonly string _rootOutputDirectory;

    public GmailServiceWrapper(
        ILogger logger,
        ICsvConverter csvConverter,
        IGmailDownloader gmailDownloader,
        IOptions<CsvFormatSettings> options)
    {
        _logger = logger;
        _csvConverter = csvConverter;
        _gmailDownloader = gmailDownloader;
        _rootOutputDirectory = options.Value.RootDirectory;
    }

    public async Task Do(IEnumerable<GmailQuery> queries, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, List<string>>();
        var totalRows = 0;

        foreach (var gmailQuery in queries)
        {
            var labelCsvs = new List<string>();
            var a = _gmailDownloader.DownloadMails(gmailQuery, cancellationToken);
            await foreach (var gmail in a.WithCancellation(cancellationToken))
            {
                var csv = _csvConverter.Create(gmail);
                labelCsvs.Add(csv);
                _logger.Information("message parsed. id={Id},csv={Csv}", gmail.Id, csv);
                totalRows++;
            }

            _logger.Information("Label processed. label={Label},count={Count}", gmailQuery.Label, labelCsvs.Count);

            results.Add(gmailQuery.Label, labelCsvs);
        }

        if (!Directory.Exists(_rootOutputDirectory))
            Directory.CreateDirectory(_rootOutputDirectory);
        
        await WriteCsvData(results, totalRows);
    }

    private async Task WriteCsvData(Dictionary<string, List<string>> mails, int totalRows)
    {
        _logger.Information("Writing labels to csv. count={LabelCount},totalRows={Total}", mails.Count, totalRows);

        const string header = "From ; Subject ; Label ; Date ; Time";

        var now = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var totalCsv = new List<string>(totalRows + 1) { header };

        foreach (var (label, csvList) in mails)
        {
            var file = $"{label}-{csvList.Count}-{now}.csv";
            var outputDirectory = Path.Combine(_rootOutputDirectory, label);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            
            var outFile = Path.Combine(outputDirectory, file);

            if (csvList.Count == 0)
            {
                _logger.Warning("No date to write, skipping. file={OutFile}", outFile);
                continue;
            }
            
            _logger.Information("Writing data. file={OutFile},label={Label},rows={Rows}",
                outFile, label, csvList.Count + 1);

            await using var writer = File.CreateText(outFile);
            await writer.WriteLineAsync(header).ConfigureAwait(false);

            foreach (var csv in csvList)
            {
                await writer.WriteLineAsync(csv).ConfigureAwait(false);
                totalCsv.Add(csv);
            }
        }

        var totalOutFile = Path.Combine(_rootOutputDirectory, $"{totalCsv.Count}-{now}.csv");
        
        _logger.Information("Writing full csv. file={OutFile}.csv,rows={Rows}", totalOutFile, totalCsv.Count);

        await using var fullWriter = File.CreateText(totalOutFile);
        foreach (var csv in totalCsv)
            await fullWriter.WriteLineAsync(csv).ConfigureAwait(false);
        
        _logger.Information("Done..");
    }
}