using System;
using System.Linq;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using YinaCRM.Core.Entities.Hardware;
using YinaCRM.Core.Entities.Hardware.Events;

namespace YinaCRM.Core.Tests;

public sealed class HardwareTests
{
    [Fact]
    public void CreateLinkUnlinkAndUpdateSnapshot()
    {
        var hardware = DomainTestHelper.ExpectValue(Hardware.Create(
            DomainTestHelper.ExternalHardwareId(),
            DomainTestHelper.HardwareType(),
            DomainTestHelper.HardwareDetailType(),
            serialNumber: DomainTestHelper.SerialNumber(),
            brand: DomainTestHelper.Brand(),
            model: DomainTestHelper.Model(),
            ipCom: DomainTestHelper.IpAddress(),
            anyDeskId: DomainTestHelper.AnyDesk(),
            anyDeskPassword: DomainTestHelper.Secret(),
            lastSeenAt: DateTime.UtcNow));

        Assert.Null(hardware.ClientId);

        // Cannot update snapshot when not linked
        var snapshotFail = hardware.UpdateSnapshot(
            DomainTestHelper.SerialNumber("SN-1000"),
            DomainTestHelper.Brand("Globex"),
            DomainTestHelper.Model("G-2000"),
            DomainTestHelper.IpAddress("10.0.0.1"),
            DateOnly.FromDateTime(DateTime.UtcNow),
            true,
            DomainTestHelper.AnyDesk("987654321"),
            DomainTestHelper.Secret("AnotherSecret1!"),
            DateTime.UtcNow);
        Assert.True(snapshotFail.IsFailure);
        Assert.Equal("HARDWARE_NOT_LINKED", snapshotFail.Error.Code);

        var clientId = ClientId.New();
        Assert.True(hardware.LinkToClient(clientId).IsSuccess);
        var linkEvent = Assert.IsType<HardwareLinkedToClient>(hardware.DequeueEvents().Single());
        Assert.Equal(clientId, linkEvent.ClientId);

        Assert.True(hardware.UpdateSnapshot(
            DomainTestHelper.SerialNumber("SN-1000"),
            DomainTestHelper.Brand("Globex"),
            DomainTestHelper.Model("G-2000"),
            DomainTestHelper.IpAddress("10.0.0.1"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            true,
            DomainTestHelper.AnyDesk("987654321"),
            DomainTestHelper.Secret("AnotherSecret1!"),
            DateTime.UtcNow).IsSuccess);
        Assert.IsType<HardwareSnapshotUpdated>(hardware.DequeueEvents().Single());

        Assert.True(hardware.UnlinkFromClient().IsSuccess);
        Assert.IsType<HardwareUnlinkedFromClient>(hardware.DequeueEvents().Single());

        // Unlink when already unlinked is a no-op
        Assert.True(hardware.UnlinkFromClient().IsSuccess);
        Assert.Empty(hardware.DequeueEvents());
    }
}
