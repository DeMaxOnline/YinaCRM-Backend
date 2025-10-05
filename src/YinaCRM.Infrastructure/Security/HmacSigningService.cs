using System.Security.Cryptography;
using System.Text;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Security;
using YinaCRM.Infrastructure.Abstractions.Secrets;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Security;

public sealed class HmacSigningService : ISigningService
{
    private readonly ISecretStore _secretStore;

    public HmacSigningService(ISecretStore secretStore)
    {
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }

    public async Task<Result<string>> SignAsync(SignRequest request, CancellationToken cancellationToken = default)
    {
        var secretName = BuildSecretName(request.TenantId, request.Algorithm, request.KeyId);
        var secretResult = await _secretStore.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        if (secretResult.IsFailure)
        {
            return Result.Failure<string>(secretResult.Error);
        }

        var signature = ComputeSignature(request.Payload, secretResult.Value, request.Algorithm);
        return Result.Success(signature);
    }

    public async Task<Result<bool>> VerifyAsync(VerifySignatureRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            return Result.Failure<bool>(InfrastructureErrors.ValidationFailure("Signature missing."));
        }

        var secretName = BuildSecretName(request.TenantId, request.Algorithm, request.KeyId);
        var secretResult = await _secretStore.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        if (secretResult.IsFailure)
        {
            return Result.Failure<bool>(secretResult.Error);
        }

        var expected = ComputeSignature(request.Payload, secretResult.Value, request.Algorithm);
        var provided = request.Signature.Trim().ToLowerInvariant();

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));

        return Result.Success(matches);
    }

    private static string ComputeSignature(string payload, string secret, string algorithm)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var keyBytes = Encoding.UTF8.GetBytes(secret);

        using var hmac = algorithm.ToLowerInvariant() switch
        {
            "sha256" => (HMAC)new HMACSHA256(keyBytes),
            "sha512" => new HMACSHA512(keyBytes),
            _ => throw new NotSupportedException($"Unsupported signing algorithm '{algorithm}'."),
        };

        var signature = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(signature).ToLowerInvariant();
    }

    private static string BuildSecretName(string tenantId, string algorithm, string? keyId)
        => $"signing:{tenantId}:{algorithm}:{keyId ?? "default"}";
}
