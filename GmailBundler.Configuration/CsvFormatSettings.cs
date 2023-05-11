namespace GmailBundler.Configuration;

public sealed class CsvFormatSettings
{
    public char Delimiter { get; init; } = ';';

    public string DateFormat { get; init; } = null!;

    public string TimeFormat { get; init; } = null!;
}