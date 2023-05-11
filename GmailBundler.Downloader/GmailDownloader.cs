using System.Runtime.CompilerServices;
using GmailBundler.Downloader.Interfaces;
using GmailBundler.Dto;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Serilog;

namespace GmailBundler.Downloader;

public sealed class GmailDownloader : IGmailDownloader
{
    private const string UserId = "me";

    private readonly ILogger _logger;
    private readonly GmailService _gmailService;

    public GmailDownloader(ILogger logger, GmailService gmailService)
    {
        _logger = logger;
        _gmailService = gmailService;
    }

    public async IAsyncEnumerable<Gmail> DownloadMails(GmailQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Downloading messages. query={Query}", query.Query);

        var pageToken = string.Empty;
        
        do
        {
            var listRequest = CreateListRequest(query, pageToken);

            var (validMessageResponse, messagesResponse) = await GetListMessageResponse(query, listRequest, cancellationToken);

            if (!validMessageResponse)
            {
                _logger.Warning("Invalid message response received. query={Query}", query.Query);
                yield break;
            }

            var messageIds = messagesResponse!.Messages.Select(x => x.Id);

            foreach (var messageId in messageIds)
            {
                var message = await _gmailService
                    .Users
                    .Messages
                    .Get(UserId, messageId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                yield return ConvertGmail(query.Label, message);
            }

            pageToken = messagesResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));
    }

    private UsersResource.MessagesResource.ListRequest CreateListRequest(GmailQuery query, string? pageToken)
    {
        var listRequest = _gmailService
            .Users
            .Messages
            .List(UserId);

        listRequest.MaxResults = 100;
        listRequest.Q = query.Query;

        if (!string.IsNullOrWhiteSpace(pageToken))
            listRequest.PageToken = pageToken;

        return listRequest;
    }
    
    private async ValueTask<(bool, ListMessagesResponse?)> GetListMessageResponse(
        GmailQuery query,
        UsersResource.MessagesResource.ListRequest listRequest,
        CancellationToken cancellationToken)
    {
        var messagesResponse = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var anyMessages = messagesResponse != null && messagesResponse.Messages.Count > 0;
        
        if (!anyMessages)
            _logger.Information("No messages found. query={Query}", query.Query);

        return (anyMessages, messagesResponse);
    }
    
    private static Gmail ConvertGmail(string label, Message emailDetails)
    {
        var dateTime = emailDetails.InternalDate.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(emailDetails.InternalDate.Value).DateTime
            : default;

        return new Gmail(
            emailDetails.Id,
            ResolveFrom(GetHeader(emailDetails, "From")),
            GetHeader(emailDetails, "Subject"),
            label,
            dateTime
        );
    }

    private static string ResolveFrom(string from)
    {
        var endIndicator = from.LastIndexOf('>');

        if (endIndicator == -1)
            return from;

        var startIndicator = from.IndexOf('<');

        return startIndicator == -1
            ? from.Replace(';', ':')
            : from.Substring(startIndicator + 1, endIndicator - startIndicator - 1);
    }

    private static string GetHeader(Message message, string headerName)
    {
        var header = message.Payload.Headers.FirstOrDefault(h => h.Name == headerName);
        return header != null ? header.Value : string.Empty;
    }
}