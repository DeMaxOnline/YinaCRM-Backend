using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace YinaCRM.Infrastructure.Auth.Auth0;

internal sealed class Auth0ConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
{
    private readonly IOptionsMonitor<Auth0Options> _optionsMonitor;
    private ConfigurationManager<OpenIdConnectConfiguration> _inner;
    private readonly object _sync = new();

    public Auth0ConfigurationManager(IOptionsMonitor<Auth0Options> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _inner = CreateManager(optionsMonitor.CurrentValue);
        _optionsMonitor.OnChange(options => ReplaceManager(options));
    }

    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        => _inner.GetConfigurationAsync(cancel);

    public void RequestRefresh() => _inner.RequestRefresh();

    private void ReplaceManager(Auth0Options options)
    {
        var manager = CreateManager(options);
        lock (_sync)
        {
            _inner = manager;
        }
    }

    private static ConfigurationManager<OpenIdConnectConfiguration> CreateManager(Auth0Options options)
    {
        var metadataAddress = Auth0Endpoints.BuildOidcConfigurationEndpoint(options.Domain).ToString();
        var retriever = new HttpDocumentRetriever
        {
            RequireHttps = options.RequireHttpsMetadata,
        };

        var manager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            retriever)
        {
            AutomaticRefreshInterval = options.JwksRefreshInterval,
            RefreshInterval = options.JwksRefreshInterval,
        };

        return manager;
    }
}
