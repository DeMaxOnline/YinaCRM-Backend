using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Auth.Auth0;

namespace YinaCRM.Infrastructure.Tests;

public class Auth0TokenVerifierTests
{
    private const string Domain = "https://yina-dev.auth0.local";
    private static string DomainWithTrailingSlash => Domain.EndsWith("/", StringComparison.Ordinal) ? Domain : Domain + "/";

    [Fact]
    public async Task VerifyAsync_ReturnsPrincipal_WhenTokenValid()
    {
        var options = CreateOptions();
        var (configuration, signingCredentials) = CreateConfiguration();
        var configurationManager = new StubConfigurationManager(configuration);
        var monitor = new StubOptionsMonitor<Auth0Options>(options);
        var verifier = new Auth0TokenVerifier(monitor, configurationManager, NullLogger<Auth0TokenVerifier>.Instance);

        var token = CreateJwt(signingCredentials, options, tenantId: "tenant-1");

        var result = await verifier.VerifyAsync(token, default);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("user-123", result.Value.SubjectId);
        Assert.Equal("tenant-1", result.Value.TenantId);
        Assert.Contains("admin", result.Value.Roles);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFailure_WhenTenantMissingAndRequired()
    {
        var options = CreateOptions();
        var (configuration, signingCredentials) = CreateConfiguration();
        var configurationManager = new StubConfigurationManager(configuration);
        var monitor = new StubOptionsMonitor<Auth0Options>(options);
        var verifier = new Auth0TokenVerifier(monitor, configurationManager, NullLogger<Auth0TokenVerifier>.Instance);

        var token = CreateJwt(signingCredentials, options, tenantId: null);
        var result = await verifier.VerifyAsync(token, default);

        Assert.True(result.IsFailure);
        Assert.Equal("AUTHENTICATION_FAILED", result.Error.Code);
    }

    [Fact]
    public async Task VerifyAsync_RequestsRefresh_WhenSigningKeyMissing()
    {
        var options = CreateOptions();
        var (configuration, _) = CreateConfiguration();
        var configurationManager = new RefreshTrackingConfigurationManager(configuration);
        var monitor = new StubOptionsMonitor<Auth0Options>(options);
        var verifier = new Auth0TokenVerifier(monitor, configurationManager, NullLogger<Auth0TokenVerifier>.Instance);

        var otherKey = new SigningCredentials(new RsaSecurityKey(RSA.Create(2048)), SecurityAlgorithms.RsaSha256);
        var token = CreateJwt(otherKey, options, tenantId: "tenant-1");

        var result = await verifier.VerifyAsync(token, default);

        Assert.True(result.IsFailure);
        Assert.Equal("AUTHENTICATION_FAILED", result.Error.Code);
        Assert.True(configurationManager.RefreshRequested);
    }

    private static Auth0Options CreateOptions() => new()
    {
        Domain = Domain,
        Audience = "https://api.yina.local",
        ClientId = "client",
        ClientSecret = "secret",
        Management = new Auth0Options.ManagementApiOptions
        {
            Audience = "https://yina-dev.auth0.local/api/v2/",
            ClientId = "mgmt",
            ClientSecret = "mgmt-secret",
        },
    };

    private static (OpenIdConnectConfiguration Configuration, SigningCredentials Credentials) CreateConfiguration()
    {
        var key = RSA.Create(2048);
        var securityKey = new RsaSecurityKey(key) { KeyId = Guid.NewGuid().ToString("N") };
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var configuration = new OpenIdConnectConfiguration
        {
            Issuer = Domain,
        };

        configuration.SigningKeys.Add(securityKey);

        return (configuration, credentials);
    }

    private static string CreateJwt(SigningCredentials credentials, Auth0Options options, string? tenantId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new("sub", "user-123"),
            new(options.EmailClaim, "user@example.com"),
            new(options.NameClaim, "Jane Example"),
            new("scope", "crm:read crm:write"),
        };

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            claims.Add(new Claim(options.TenantIdClaim, tenantId));
        }

        foreach (var roleClaim in options.RoleClaims)
        {
            claims.Add(new Claim(roleClaim, "admin"));
        }

        var token = new JwtSecurityToken(
            issuer: DomainWithTrailingSlash,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return tokenHandler.WriteToken(token);
    }

    private sealed class StubConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
    {
        private readonly OpenIdConnectConfiguration _configuration;

        public StubConfigurationManager(OpenIdConnectConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) => Task.FromResult(_configuration);

        public void RequestRefresh()
        {
        }
    }

    private sealed class RefreshTrackingConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
    {
        private readonly OpenIdConnectConfiguration _configuration;

        public RefreshTrackingConfigurationManager(OpenIdConnectConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool RefreshRequested { get; private set; }

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) => Task.FromResult(_configuration);

        public void RequestRefresh() => RefreshRequested = true;
    }
}


