using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;

namespace YinaCRM.Core.Entities.ClientEnvironment.Events;

public sealed record EnvironmentUrlAdded(
    ClientEnvironmentId EnvironmentId,
    Guid UrlId,
    UrlTypeCode TypeCode,
    Url Url,
    bool IsPrimary) : DomainEventBase(EnvironmentId.ToString(), nameof(ClientEnvironment))
{
}


