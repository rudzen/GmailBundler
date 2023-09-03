using GmailBundler.Domain.Models;
using GmailBundler.Domain.Services;
using GmailBundler.Domain.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace GmailBundler.App.Host;

public class GmailBundlerHost : IHostedService
{
    private readonly IGmailServiceWrapper _gmailServiceWrapper;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly GoogleApiSettings _googleApiSettings;

    private readonly ILogger _logger;
    
    public GmailBundlerHost(
        ILogger logger,
        IGmailServiceWrapper gmailServiceWrapper,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<GoogleApiSettings> googleApiSettings)
    {
        _logger = logger;
        _gmailServiceWrapper = gmailServiceWrapper;
        _applicationLifetime = hostApplicationLifetime;
        _googleApiSettings = googleApiSettings.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var queries = CreateQueries(_googleApiSettings.Labels).ToArray();

        if (queries is not { Length: > 0 })
        {
            _logger.Warning("No queries found");
            return;
        }

        await _gmailServiceWrapper.Do(queries, cancellationToken);
        
        _applicationLifetime.StopApplication();
    }

    private static IEnumerable<GmailQuery> CreateQueries(IEnumerable<string> labels)
    {
        // Add label filters from the configuration, use OR between multiple labels if needed
        return labels.Select(label => new GmailQuery($"label:{label}", label));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}