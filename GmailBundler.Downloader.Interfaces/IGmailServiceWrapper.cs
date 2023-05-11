using GmailBundler.Dto;

namespace GmailBundler.Downloader.Interfaces;

public interface IGmailServiceWrapper
{
    Task Do(IEnumerable<GmailQuery> queries, CancellationToken cancellationToken);
}