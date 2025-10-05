using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;

namespace YinaCRM.Core.Entities.Client.Events;

public sealed record ClientPrimaryEmailChanged(
    ClientId ClientId,
    Email? OldEmail,
    Email? NewEmail) : DomainEventBase(ClientId.ToString(), nameof(Client))
{
}


