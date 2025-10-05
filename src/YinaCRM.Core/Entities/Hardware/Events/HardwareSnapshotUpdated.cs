using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Hardware.Events;

public sealed record HardwareSnapshotUpdated(
    HardwareId HardwareId) : DomainEventBase(HardwareId.ToString(), nameof(Hardware))
{
}


