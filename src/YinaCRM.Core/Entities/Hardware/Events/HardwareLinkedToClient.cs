using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Hardware.Events;

public sealed record HardwareLinkedToClient(
    HardwareId HardwareId,
    ClientId ClientId) : DomainEventBase(HardwareId.ToString(), nameof(Hardware))
{
}


