using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Security;

public interface ISigningService
{
    Task<Result<string>> SignAsync(SignRequest request, CancellationToken cancellationToken = default);

    Task<Result<bool>> VerifyAsync(VerifySignatureRequest request, CancellationToken cancellationToken = default);
}

public sealed record SignRequest(
    string TenantId,
    string Payload,
    string Algorithm,
    string? KeyId,
    IReadOnlyDictionary<string, string> Headers);

public sealed record VerifySignatureRequest(
    string TenantId,
    string Payload,
    string Signature,
    string Algorithm,
    string? KeyId,
    IReadOnlyDictionary<string, string> Headers);
