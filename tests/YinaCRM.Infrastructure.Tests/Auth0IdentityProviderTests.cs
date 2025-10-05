using System.Net;
using System.Text;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Auth.Auth0;

namespace YinaCRM.Infrastructure.Tests;

public class Auth0IdentityProviderTests
{
    [Fact]
    public async Task ExchangeCodeAsync_ReturnsTokens()
    {
        var handler = new TestHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/oauth/token", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    access_token = "access",
                    id_token = "id",
                    refresh_token = "refresh",
                    expires_in = 3600,
                    scope = "crm:read",
                }),
            };
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://auth0.local"),
        };

        var options = new StubOptionsMonitor<Auth0Options>(new Auth0Options
        {
            Domain = "https://auth0.local",
            Audience = "https://api",
            ClientId = "client",
            ClientSecret = "secret",
            Management = new Auth0Options.ManagementApiOptions
            {
                Audience = "https://auth0.local/api/v2/",
                ClientId = "mgmt",
                ClientSecret = "mgmt-secret",
            }
        });

        var provider = new Auth0IdentityProvider(httpClient, options, NullLogger<Auth0IdentityProvider>.Instance);

        var result = await provider.ExchangeCodeAsync(new CodeExchangeRequest("code", "https://app/callback", null, "tenant"));
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal("id", result.Value.IdToken);
        Assert.Equal("refresh", result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsOriginalRefreshToken_WhenMissing()
    {
        var handler = new TestHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                access_token = "new-access",
                expires_in = 3600,
            }),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth0.local") };
        var options = new StubOptionsMonitor<Auth0Options>(new Auth0Options
        {
            Domain = "https://auth0.local",
            Audience = "https://api",
            ClientId = "client",
            ClientSecret = "secret",
            Management = new Auth0Options.ManagementApiOptions
            {
                Audience = "https://auth0.local/api/v2/",
                ClientId = "mgmt",
                ClientSecret = "mgmt-secret",
            }
        });
        var provider = new Auth0IdentityProvider(httpClient, options, NullLogger<Auth0IdentityProvider>.Instance);

        var result = await provider.RefreshAsync(new TokenRefreshRequest("refresh-original", "tenant"));
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("refresh-original", result.Value.RefreshToken);
    }

    [Fact]
    public async Task RevokeAsync_ReturnsFailure_OnHttpError()
    {
        var handler = new TestHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new { error = "invalid" }),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth0.local") };
        var options = new StubOptionsMonitor<Auth0Options>(new Auth0Options
        {
            Domain = "https://auth0.local",
            Audience = "https://api",
            ClientId = "client",
            ClientSecret = "secret",
            Management = new Auth0Options.ManagementApiOptions
            {
                Audience = "https://auth0.local/api/v2/",
                ClientId = "mgmt",
                ClientSecret = "mgmt-secret",
            }
        });
        var provider = new Auth0IdentityProvider(httpClient, options, NullLogger<Auth0IdentityProvider>.Instance);

        var result = await provider.RevokeAsync(new TokenRevokeRequest("token", TokenRevokeType.RefreshToken, "tenant"));
        Assert.True(result.IsFailure);
    }
}

