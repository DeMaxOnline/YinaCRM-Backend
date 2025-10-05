using YinaCRM.Core.Entities.Client.VOs;
using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Client.Events;

public sealed record ClientCreated(
    ClientId ClientId,
    int YinaYinaId,
    InternalName InternalName,
    DateTime CreatedAtUtc) : DomainEventBase(ClientId.ToString(), nameof(Client))
{
}


