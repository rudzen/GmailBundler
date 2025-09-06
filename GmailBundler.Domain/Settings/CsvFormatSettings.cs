namespace GmailBundler.Domain.Settings;

public sealed class CsvFormatSettings
{
    public char Delimiter { get; init; } = ';';

    public string DateFormat { get; init; } = null!;

    public string TimeFormat { get; init; } = null!;

    public string RootDirectory { get; init; } = ".";
}