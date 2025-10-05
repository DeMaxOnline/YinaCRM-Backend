using YinaCRM.Core.Entities.Client.VOs;
using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Client.Events;

public sealed record ClientRenamed(
    ClientId ClientId,
    InternalName OldName,
    InternalName NewName) : DomainEventBase(ClientId.ToString(), nameof(Client))
{
}


