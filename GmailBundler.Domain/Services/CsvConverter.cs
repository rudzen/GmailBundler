using GmailBundler.Domain.Models;
using GmailBundler.Domain.Settings;
using Microsoft.Extensions.Options;
using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using Serilog;

namespace GmailBundler.Domain.Services;

public interface ICsvConverter
{
    string Create(Gmail gmail);
}

public sealed class CsvConverter : ICsvConverter
{
    private readonly ILogger _logger;
    private readonly IVariableLengthWriter<Gmail> _writer;

    public CsvConverter(ILogger logger, IOptions<CsvFormatSettings> options)
    {
        _logger = logger;
        var config = options.Value;
        _writer = new VariableLengthWriterBuilder<Gmail>()
                  .Map(x => x.From, indexColumn: 0)
                  .Map(x => x.Subject.Replace(';', ':'), indexColumn: 1)
                  .Map(x => x.Label, indexColumn: 2)
                  .Map(x => DateOnly.FromDateTime(x.Date), indexColumn: 3, format: config.DateFormat)
                  .Map(x => x.Date, indexColumn: 4, format: config.TimeFormat)
                  .Build($" {config.Delimiter} ");
    }

    public string Create(Gmail gmail)
    {
        var length = gmail.From.Length + gmail.Subject.Length + gmail.Label.Length + 50;

        Span<char> destination = length > 1024 ? new char[length] : stackalloc char[length];
        var success = _writer.TryFormat(gmail, destination, out var written);
        if (success)
            return destination[..written].ToString();

        _logger.Warning("unable to write csv");
        return string.Empty;
    }
}