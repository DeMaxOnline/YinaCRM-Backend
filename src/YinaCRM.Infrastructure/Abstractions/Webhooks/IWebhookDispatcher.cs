using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Webhooks;

public interface IWebhookDispatcher
{
    Task<Result<WebhookDispatchResult>> DispatchAsync(WebhookDispatchRequest request, CancellationToken cancellationToken = default);
}

public sealed record WebhookDispatchRequest(
    string Endpoint,
    string Secret,
    string TenantId,
    string EventType,
    string Payload,
    IReadOnlyDictionary<string, string> Headers,
    TimeSpan Timeout,
    int MaxAttempts);

public sealed record WebhookDispatchResult(
    bool Delivered,
    int Attempts,
    int? LastStatusCode,
    DateTimeOffset? DeliveredAtUtc,
    IReadOnlyList<string> FailureReasons);

public interface IWebhookSignatureVerifier
{
    Result VerifySignature(WebhookVerificationContext context);
}

public sealed record WebhookVerificationContext(
    string Secret,
    string Signature,
    string Payload,
    string Algorithm,
    DateTimeOffset ReceivedAtUtc,
    string? TenantId);
