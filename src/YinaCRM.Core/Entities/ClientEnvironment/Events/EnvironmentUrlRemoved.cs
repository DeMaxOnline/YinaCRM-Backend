using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.ClientEnvironment.Events;

public sealed record EnvironmentUrlRemoved(
    ClientEnvironmentId EnvironmentId,
    Guid UrlId) : DomainEventBase(EnvironmentId.ToString(), nameof(ClientEnvironment))
{
}


