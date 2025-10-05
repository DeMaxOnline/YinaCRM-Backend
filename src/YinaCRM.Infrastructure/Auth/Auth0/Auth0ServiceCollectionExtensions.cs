using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Polly;
using Polly.Extensions.Http;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Abstractions.Webhooks;

namespace YinaCRM.Infrastructure.Auth.Auth0;

public static class Auth0ServiceCollectionExtensions
{
    private const string IdentityHttpClientName = "Auth0.Identity";
    private const string ManagementTokenClientName = "Auth0.Management.Token";
    private const string ManagementApiClientName = "Auth0.Management.Api";

    public static IServiceCollection AddAuth0Infrastructure(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Auth0")
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configuration is not null)
        {
            services.AddOptions<Auth0Options>()
                .Bind(configuration.GetSection(sectionName))
                .ValidateDataAnnotations()
                .Validate(options => !string.IsNullOrWhiteSpace(options.Domain), "Domain is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Audience is required.");
        }
        else
        {
            services.AddOptions<Auth0Options>()
                .ValidateDataAnnotations()
                .Validate(options => !string.IsNullOrWhiteSpace(options.Domain), "Domain is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Audience is required.");
        }

        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>, Auth0ConfigurationManager>();

        services.AddHttpClient(IdentityHttpClientName, ConfigureAuth0Client)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient(ManagementTokenClientName, ConfigureAuth0Client)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient(ManagementApiClientName, ConfigureAuth0Client)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddSingleton<Auth0ManagementTokenProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(ManagementTokenClientName);
            var logger = sp.GetRequiredService<ILogger<Auth0ManagementTokenProvider>>();
            var options = sp.GetRequiredService<IOptionsMonitor<Auth0Options>>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new Auth0ManagementTokenProvider(client, options, logger, timeProvider);
        });

        services.AddSingleton<IWebhookSignatureVerifier, Auth0WebhookVerifier>();

        services.AddScoped<IIdentityProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(IdentityHttpClientName);
            var options = sp.GetRequiredService<IOptionsMonitor<Auth0Options>>();
            var logger = sp.GetRequiredService<ILogger<Auth0IdentityProvider>>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new Auth0IdentityProvider(client, options, logger, timeProvider);
        });

        services.AddSingleton<ITokenVerifier>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<Auth0Options>>();
            var configurationManager = sp.GetRequiredService<IConfigurationManager<OpenIdConnectConfiguration>>();
            var logger = sp.GetRequiredService<ILogger<Auth0TokenVerifier>>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new Auth0TokenVerifier(options, configurationManager, logger, timeProvider);
        });

        services.AddScoped<IUserDirectory>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(ManagementApiClientName);
            var tokenProvider = sp.GetRequiredService<Auth0ManagementTokenProvider>();
            var logger = sp.GetRequiredService<ILogger<Auth0UserDirectory>>();
            return new Auth0UserDirectory(client, tokenProvider, logger);
        });

        return services;
    }

    private static void ConfigureAuth0Client(IServiceProvider provider, HttpClient client)
    {
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<Auth0Options>>();
        var options = optionsMonitor.CurrentValue;
        client.BaseAddress = Auth0Endpoints.BuildAuthority(options.Domain);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(30);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)));
    }
}

