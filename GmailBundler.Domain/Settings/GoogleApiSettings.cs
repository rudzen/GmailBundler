namespace GmailBundler.Domain.Settings;

public sealed class GoogleApiSettings
{
    public string ApplicationName { get; init; } = null!;
    public string ClientId { get; init; } = null!;
    public string ClientSecret { get; init; } = null!;
    public List<string> Labels { get; init; } = null!;
}