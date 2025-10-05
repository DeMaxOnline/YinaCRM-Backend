using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.Hardware.Events;
using YinaCRM.Core.Entities.Hardware.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects.Identity.SecretVO;

namespace YinaCRM.Core.Entities.Hardware;

/// <summary>
/// Hardware entity synchronized from an external API. Snapshot-style mutable fields updated from upstream.
/// </summary>
public sealed class Hardware : AggregateRoot<HardwareId>
{

    private Hardware()
    {
        // Required by EF Core
    }

    private Hardware(
        HardwareId id,
        ExternalHardwareId externalId,
        HardwareTypeCode typeCode,
        HardwareDetailTypeCode detailTypeCode,
        ClientId? clientId,
        SerialNumber? serialNumber,
        Brand? brand,
        Model? model,
        IpAddress? ipCom,
        DateOnly? warrantyDate,
        bool deliveredByUs,
        AnyDeskId? anyDeskId,
        Secret? anyDeskPassword,
        DateTime? lastSeenAt,
        DateTime createdAtUtc)
    {
        Id = id;
        ExternalHardwareId = externalId;
        TypeCode = typeCode;
        DetailTypeCode = detailTypeCode;
        ClientId = clientId;
        SerialNumber = serialNumber;
        Brand = brand;
        Model = model;
        IpCom = ipCom;
        WarrantyDate = warrantyDate;
        DeliveredByUs = deliveredByUs;
        AnyDeskId = anyDeskId;
        AnyDeskPassword = anyDeskPassword;
        LastSeenAt = lastSeenAt.HasValue ? DateTime.SpecifyKind(lastSeenAt.Value, DateTimeKind.Utc) : null;
        CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        UpdatedAt = null;
    }

    public override HardwareId Id { get; protected set; }
    public ClientId? ClientId { get; private set; }
    public ExternalHardwareId ExternalHardwareId { get; private set; }
    public HardwareTypeCode TypeCode { get; private set; }
    public HardwareDetailTypeCode DetailTypeCode { get; private set; }
    public SerialNumber? SerialNumber { get; private set; }
    public Brand? Brand { get; private set; }
    public Model? Model { get; private set; }
    public IpAddress? IpCom { get; private set; }
    public DateOnly? WarrantyDate { get; private set; }
    public bool DeliveredByUs { get; private set; }
    public AnyDeskId? AnyDeskId { get; private set; }
    public Secret? AnyDeskPassword { get; private set; }
    public DateTime? LastSeenAt { get; private set; }


    // Factory (autogenerate id)
    public static Result<Hardware> Create(
        ExternalHardwareId externalId,
        HardwareTypeCode typeCode,
        HardwareDetailTypeCode detailTypeCode,
        ClientId? clientId = null,
        SerialNumber? serialNumber = null,
        Brand? brand = null,
        Model? model = null,
        IpAddress? ipCom = null,
        DateOnly? warrantyDate = null,
        bool deliveredByUs = false,
        AnyDeskId? anyDeskId = null,
        Secret? anyDeskPassword = null,
        DateTime? lastSeenAt = null,
        DateTime? createdAtUtc = null)
        => Create(
            HardwareId.New(),
            externalId,
            typeCode,
            detailTypeCode,
            clientId,
            serialNumber,
            brand,
            model,
            ipCom,
            warrantyDate,
            deliveredByUs,
            anyDeskId,
            anyDeskPassword,
            lastSeenAt,
            createdAtUtc);

    // Factory (explicit id)
    public static Result<Hardware> Create(
        HardwareId id,
        ExternalHardwareId externalId,
        HardwareTypeCode typeCode,
        HardwareDetailTypeCode detailTypeCode,
        ClientId? clientId = null,
        SerialNumber? serialNumber = null,
        Brand? brand = null,
        Model? model = null,
        IpAddress? ipCom = null,
        DateOnly? warrantyDate = null,
        bool deliveredByUs = false,
        AnyDeskId? anyDeskId = null,
        Secret? anyDeskPassword = null,
        DateTime? lastSeenAt = null,
        DateTime? createdAtUtc = null)
    {
        if (externalId.IsEmpty) return Result<Hardware>.Failure(Errors.ExternalIdRequired());
        if (typeCode.IsEmpty) return Result<Hardware>.Failure(Errors.TypeCodeRequired());
        if (detailTypeCode.IsEmpty) return Result<Hardware>.Failure(Errors.DetailTypeCodeRequired());

        var hw = new Hardware(
            id,
            externalId,
            typeCode,
            detailTypeCode,
            clientId,
            serialNumber,
            brand,
            model,
            ipCom,
            warrantyDate,
            deliveredByUs,
            anyDeskId,
            anyDeskPassword,
            lastSeenAt,
            createdAtUtc ?? DateTime.UtcNow);

        return Result<Hardware>.Success(hw);
    }

    public Result LinkToClient(ClientId clientId)
    {
        if (ClientId is { } existing && existing.Equals(clientId))
            return Result.Success();

        ClientId = clientId;
        RaiseEvent(new HardwareLinkedToClient(Id, clientId));
        return Result.Success();
    }

    public Result UnlinkFromClient()
    {
        if (ClientId is null) return Result.Success();

        var old = ClientId.Value;
        ClientId = null;
        RaiseEvent(new HardwareUnlinkedFromClient(Id, old));
        return Result.Success();
    }

    public Result UpdateSnapshot(
        SerialNumber? serialNumber,
        Brand? brand,
        Model? model,
        IpAddress? ipCom,
        DateOnly? warrantyDate,
        bool deliveredByUs,
        AnyDeskId? anyDeskId,
        Secret? anyDeskPassword,
        DateTime? lastSeenAt)
    {
        if (ClientId is null)
            return Result.Failure(Errors.NotLinkedToClient());

        SerialNumber = serialNumber;
        Brand = brand;
        Model = model;
        IpCom = ipCom;
        WarrantyDate = warrantyDate;
        DeliveredByUs = deliveredByUs;
        AnyDeskId = anyDeskId;
        AnyDeskPassword = anyDeskPassword;
        LastSeenAt = lastSeenAt.HasValue ? DateTime.SpecifyKind(lastSeenAt.Value, DateTimeKind.Utc) : null;

        RaiseEvent(new HardwareSnapshotUpdated(Id));
        return Result.Success();
    }

    /// <summary>
    /// Applies events to rebuild the aggregate state during event sourcing replay.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    protected override void ApplyEvent(IDomainEvent @event)
    {
        switch (@event)
        {
            case HardwareLinkedToClient linked:
                ClientId = linked.ClientId;
                UpdatedAt = linked.OccurredAtUtc;
                break;

            case HardwareUnlinkedFromClient unlinked:
                ClientId = null;
                UpdatedAt = unlinked.OccurredAtUtc;
                break;

            case HardwareSnapshotUpdated updated:
                UpdatedAt = updated.OccurredAtUtc;
                break;

            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }

    private static class Errors
    {
        public static Error ExternalIdRequired() => Error.Create("HARDWARE_EXTERNALID_REQUIRED", "ExternalHardwareId is required", 400);
        public static Error TypeCodeRequired() => Error.Create("HARDWARE_TYPECODE_REQUIRED", "HardwareTypeCode is required", 400);
        public static Error DetailTypeCodeRequired() => Error.Create("HARDWARE_DETAILTYPECODE_REQUIRED", "HardwareDetailTypeCode is required", 400);
        public static Error NotLinkedToClient() => Error.Create("HARDWARE_NOT_LINKED", "Cannot update snapshot unless linked to a Client", 412);
    }
}



