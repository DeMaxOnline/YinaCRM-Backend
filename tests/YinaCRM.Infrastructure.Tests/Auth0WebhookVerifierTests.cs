using Microsoft.Extensions.Options;
using Xunit;
using YinaCRM.Infrastructure.Abstractions.Webhooks;
using YinaCRM.Infrastructure.Auth.Auth0;

namespace YinaCRM.Infrastructure.Tests;

public sealed class Auth0WebhookVerifierTests
{
    [Fact]
    public void VerifySignature_ReturnsFailure_ForInvalidSignature()
    {
        var options = new Auth0Options
        {
            Domain = "https://tenant.auth0.local",
            Audience = "https://api.yina.test",
            ClientId = "client",
            ClientSecret = "secret",
            Management = new Auth0Options.ManagementApiOptions
            {
                Audience = "https://tenant.auth0.local/api/v2/",
                ClientId = "mgmt",
                ClientSecret = "mgmt-secret"
            },
            Webhooks = new Auth0Options.WebhookOptions
            {
                Algorithm = "sha256"
            }
        };

        var verifier = new Auth0WebhookVerifier(new OptionsWrapper<Auth0Options>(options));
        var context = new WebhookVerificationContext(
            Secret: "top-secret",
            Signature: "deadbeef",
            Payload: "{}",
            Algorithm: "sha256",
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            TenantId: "tenant-1");

        var result = verifier.VerifySignature(context);
        Assert.True(result.IsFailure);
        Assert.Equal("AUTHENTICATION_FAILED", result.Error.Code);
    }

    [Fact]
    public void VerifySignature_ReturnsValidationFailure_ForUnsupportedAlgorithm()
    {
        var options = new Auth0Options
        {
            Domain = "https://tenant.auth0.local",
            Audience = "https://api.yina.test",
            ClientId = "client",
            ClientSecret = "secret",
            Management = new Auth0Options.ManagementApiOptions
            {
                Audience = "https://tenant.auth0.local/api/v2/",
                ClientId = "mgmt",
                ClientSecret = "mgmt-secret"
            },
            Webhooks = new Auth0Options.WebhookOptions
            {
                Algorithm = "sha1"
            }
        };

        var verifier = new Auth0WebhookVerifier(new OptionsWrapper<Auth0Options>(options));
        var context = new WebhookVerificationContext(
            Secret: "top-secret",
            Signature: "sha512=abc123",
            Payload: "{}",
            Algorithm: "sha512",
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            TenantId: "tenant-1");

        var result = verifier.VerifySignature(context);
        Assert.True(result.IsFailure);
        Assert.Equal("INFRA_VALIDATION_FAILED", result.Error.Code);
    }
}
