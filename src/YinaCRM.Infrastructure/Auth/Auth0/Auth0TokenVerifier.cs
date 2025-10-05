using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Auth.Auth0;

public sealed class Auth0TokenVerifier : ITokenVerifier
{
    private readonly IOptionsMonitor<Auth0Options> _optionsMonitor;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly ILogger<Auth0TokenVerifier> _logger;
    private readonly TimeProvider _timeProvider;

    public Auth0TokenVerifier(
        IOptionsMonitor<Auth0Options> optionsMonitor,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        ILogger<Auth0TokenVerifier> logger,
        TimeProvider? timeProvider = null)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<AuthenticatedPrincipal>> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.AuthenticationFailure("Token is missing."));
        }

        var options = _optionsMonitor.CurrentValue;
        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);

        var validationParameters = BuildValidationParameters(options, configuration);

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var result = handler.ValidateToken(token, validationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtToken)
            {
                return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.AuthenticationFailure("Token is not a JWT."));
            }

            var principalFactory = new Auth0PrincipalFactory(options);
            return principalFactory.Create(result);
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Auth0 signing key not found, forcing configuration refresh.");
            _configurationManager.RequestRefresh();
            return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.AuthenticationFailure("Signing key mismatch."));
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.AuthenticationFailure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying token.");
            return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.Unexpected("Token verification failed."));
        }
    }

    private TokenValidationParameters BuildValidationParameters(
        Auth0Options options,
        OpenIdConnectConfiguration configuration)
    {
        var audiences = options.EffectiveAudiences;
        var issuer = Auth0Endpoints.BuildAuthority(options.Domain).ToString();
        var issuerWithTrailingSlash = issuer.EndsWith("/", StringComparison.Ordinal) ? issuer : issuer + "/";

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuer = true,
            ValidIssuers = new[] { issuer, issuerWithTrailingSlash },
            ValidAudiences = audiences,
            ValidateAudience = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateLifetime = true,
            ClockSkew = options.ClockSkew,
            NameClaimType = options.NameClaim,
            RoleClaimType = options.RoleClaims.FirstOrDefault() ?? ClaimTypes.Role,
            ValidateTokenReplay = false,
        };
    }
}


