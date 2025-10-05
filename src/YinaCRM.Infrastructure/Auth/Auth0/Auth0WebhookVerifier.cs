using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Webhooks;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Auth.Auth0;

internal sealed class Auth0WebhookVerifier : IWebhookSignatureVerifier
{
    private readonly IOptionsMonitor<Auth0Options> _optionsMonitor;

    public Auth0WebhookVerifier(IOptionsMonitor<Auth0Options> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public Result VerifySignature(WebhookVerificationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Secret))
        {
            return Result.Failure(InfrastructureErrors.ValidationFailure("Webhook secret missing."));
        }

        if (string.IsNullOrWhiteSpace(context.Signature))
        {
            return Result.Failure(InfrastructureErrors.ValidationFailure("Webhook signature missing."));
        }

        var options = _optionsMonitor.CurrentValue;
        var algorithm = options.Webhooks.Algorithm;

        try
        {
            var expected = ComputeSignature(context.Payload, context.Secret, algorithm);
            var provided = DecodeSignature(context.Signature);

            if (expected.Length != provided.Length || !CryptographicOperations.FixedTimeEquals(expected, provided))
            {
                return Result.Failure(InfrastructureErrors.AuthenticationFailure("Invalid webhook signature."));
            }
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or NotSupportedException)
        {
            return Result.Failure(InfrastructureErrors.ValidationFailure(ex.Message));
        }

        return Result.Success();
    }

    private static byte[] ComputeSignature(string payload, string secret, string algorithm)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = algorithm.ToLowerInvariant() switch
        {
            "sha256" => (HMAC)new HMACSHA256(keyBytes),
            "sha1" => new HMACSHA1(keyBytes),
            _ => throw new NotSupportedException($"Unsupported Auth0 webhook algorithm: {algorithm}"),
        };

        return hmac.ComputeHash(payloadBytes);
    }

    private static byte[] DecodeSignature(string signature)
    {
        var cleaned = signature.Trim();
        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[2..];
        }

        if (cleaned.Contains(':'))
        {
            var parts = cleaned.Split(':', 2);
            cleaned = parts[1];
        }

        if (cleaned.Length % 2 != 0)
        {
            throw new FormatException("Signature must have an even number of hex characters.");
        }

        var buffer = new byte[cleaned.Length / 2];
        for (var i = 0; i < cleaned.Length; i += 2)
        {
            buffer[i / 2] = byte.Parse(cleaned.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return buffer;
    }
}
