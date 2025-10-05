using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Notifications;

public interface INotificationSender
{
    Task<Result> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed record NotificationMessage(
    NotificationChannel Channel,
    string Recipient,
    string TemplateId,
    IReadOnlyDictionary<string, string> Tokens,
    string? TenantId,
    string? CorrelationId);

public enum NotificationChannel
{
    Email,
    Sms,
    Voice,
    InApp,
}
