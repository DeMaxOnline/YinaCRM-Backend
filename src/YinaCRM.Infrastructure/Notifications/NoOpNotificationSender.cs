using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Notifications;

namespace YinaCRM.Infrastructure.Notifications;

public sealed class NoOpNotificationSender : INotificationSender
{
    public Task<Result> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
