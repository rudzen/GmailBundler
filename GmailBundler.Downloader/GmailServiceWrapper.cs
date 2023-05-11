using GmailBundler.Csv.Interfaces;
using GmailBundler.Downloader.Interfaces;
using GmailBundler.Dto;
using Google.Apis.Gmail.v1;
using Serilog;

namespace GmailBundler.Downloader;

public sealed class GmailServiceWrapper : IGmailServiceWrapper
{
    public static readonly string[] Scopes = { GmailService.Scope.GmailReadonly };

    private readonly ILogger _logger;
    private readonly ICsvConverter _csvConverter;
    private readonly IGmailDownloader _gmailDownloader;

    public GmailServiceWrapper(
        ILogger logger,
        ICsvConverter csvConverter,
        IGmailDownloader gmailDownloader)
    {
        _logger = logger;
        _csvConverter = csvConverter;
        _gmailDownloader = gmailDownloader;
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

        await WriteCsvData(results, totalRows);
    }

    private async Task WriteCsvData(Dictionary<string, List<string>> mails, int totalRows)
    {
        _logger.Information("Writing labels to csv. count={LabelCount},totalRows={Total}", mails.Count, totalRows);

        const string header = "From ; Subject ; Label ; Date ; Time";

        var now = DateTime.Now;
        var totalCsv = new List<string>(totalRows + 1);
        var fileName = $"gmailbundler_{now:yyyy-MM-dd_HH-mm-ss}";
        totalCsv.Add(header);

        foreach (var (label, csvList) in mails)
        {
            var outFile = $"{fileName}-{label}.csv";
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

        _logger.Information("Writing full csv. file={OutFile}.csv,rows={Rows}", fileName, totalCsv.Count);

        await using var fullWriter = File.CreateText($"{fileName}.csv");
        foreach (var csv in totalCsv)
            await fullWriter.WriteLineAsync(csv).ConfigureAwait(false);
        
        _logger.Information("Done..");
    }
}