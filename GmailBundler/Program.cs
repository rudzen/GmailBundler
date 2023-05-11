using System.Globalization;
using GmailBundler.Configuration;
using GmailBundler.Csv;
using GmailBundler.Csv.Interfaces;
using GmailBundler.Downloader;
using GmailBundler.Downloader.Interfaces;
using GmailBundler.Host;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

const string defaultCulture = "da-DK";

static ILogger CreateLogger(IConfiguration configuration)
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.WithThreadId()
        .Enrich.FromLogContext()
        .CreateLogger();
    AppDomain.CurrentDomain.ProcessExit += static (_, _) => Log.CloseAndFlush();
    return Log.Logger;
}

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(defaultCulture);

var hostBuilder = new HostBuilder()
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        config.AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: false);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<CsvFormatSettings>(hostContext.Configuration.GetSection("CsvFormat"));
        services.Configure<GoogleApiSettings>(hostContext.Configuration.GetSection("GoogleApi"));
        services.AddTransient<IGmailServiceWrapper, GmailServiceWrapper>();
        services.AddSingleton<ICsvConverter, CsvConverter>();
        services.AddSingleton<IGmailDownloader, GmailDownloader>();
        services.AddHostedService<GmailBundlerHost>();

        Log.Logger = CreateLogger(hostContext.Configuration);

        services.AddSingleton(Log.Logger);

        services.AddSingleton(sp =>
        {
            var googleApiSettings = sp.GetRequiredService<IOptions<GoogleApiSettings>>().Value;
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = googleApiSettings.ClientId,
                    ClientSecret = googleApiSettings.ClientSecret
                },
                GmailServiceWrapper.Scopes,
                "user",
                CancellationToken.None).GetAwaiter().GetResult();

            const string applicationName = "gmailbundler";

            return new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        });
    });

await hostBuilder.RunConsoleAsync();