using System.Runtime.CompilerServices;
using GmailBundler.Dto;

namespace GmailBundler.Downloader.Interfaces;

public interface IGmailDownloader
{
    IAsyncEnumerable<Gmail> DownloadMails(GmailQuery query, [EnumeratorCancellation] CancellationToken cancellationToken);
}