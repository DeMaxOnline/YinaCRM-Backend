using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.Client.Events;
using YinaCRM.Core.Entities.Client.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;
using YinaCRM.Core.ValueObjects.AddressVO.CityVO;
using YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name;
using YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
using YinaCRM.Core.ValueObjects.Identity.PhoneVO;

namespace YinaCRM.Core.Entities.Client;

/// <summary>
/// Client aggregate root (no persistence concerns). Encapsulates identity, naming, contact info, address and tags.
/// Invariants enforced via factory/mutators returning Result/Result&lt;T&gt;.
/// </summary>
public sealed partial class Client : AggregateRoot<ClientId>
{
    private readonly List<Tag> _tags = new();

        private Client()
    {
        EnsurePersistenceTagsInitialized();
    }

    private Client(
        ClientId id,
        int yinaYinaId,
        InternalName internalName,
        CompanyName? companyName,
        CommercialName? commercialName,
        Email? primaryEmail,
        Phone? primaryPhone,
        AddressLine? addressLine1,
        AddressLine? addressLine2,
        City? city,
        PostalCode? postalCode,
        CountryName? country,
        IEnumerable<Tag>? tags,
        DateTime createdAtUtc)
    {
        Id = id;
        YinaYinaId = yinaYinaId;
        InternalName = internalName;
        CompanyName = companyName;
        CommercialName = commercialName;
        PrimaryEmail = primaryEmail;
        PrimaryPhone = primaryPhone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        PostalCode = postalCode;
        Country = country;
        CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);

        if (tags is not null)
            _tags.AddRange(tags);

        EnsurePersistenceTagsInitialized();
        SyncPersistenceTagsFromDomain();
    }

    public override ClientId Id { get; protected set; }
    public int YinaYinaId { get; private set; }
    public InternalName InternalName { get; private set; }
    public CompanyName? CompanyName { get; private set; }
    public CommercialName? CommercialName { get; private set; }
    public Email? PrimaryEmail { get; private set; }
    public Phone? PrimaryPhone { get; private set; }
    public AddressLine? AddressLine1 { get; private set; }
    public AddressLine? AddressLine2 { get; private set; }
    public City? City { get; private set; }
    public PostalCode? PostalCode { get; private set; }
    public CountryName? Country { get; private set; }
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();


    // Factory overload generating a new ClientId
    public static Result<Client> Create(
        int yinaYinaId,
        InternalName internalName,
        CompanyName? companyName = null,
        CommercialName? commercialName = null,
        Email? primaryEmail = null,
        Phone? primaryPhone = null,
        AddressLine? addressLine1 = null,
        AddressLine? addressLine2 = null,
        City? city = null,
        PostalCode? postalCode = null,
        CountryName? country = null,
        IEnumerable<Tag>? tags = null,
        DateTime? createdAtUtc = null)
        => Create(
            ClientId.New(),
            yinaYinaId,
            internalName,
            companyName,
            commercialName,
            primaryEmail,
            primaryPhone,
            addressLine1,
            addressLine2,
            city,
            postalCode,
            country,
            tags,
            createdAtUtc);

    // Factory overload with explicit ClientId
    public static Result<Client> Create(
        ClientId id,
        int yinaYinaId,
        InternalName internalName,
        CompanyName? companyName = null,
        CommercialName? commercialName = null,
        Email? primaryEmail = null,
        Phone? primaryPhone = null,
        AddressLine? addressLine1 = null,
        AddressLine? addressLine2 = null,
        City? city = null,
        PostalCode? postalCode = null,
        CountryName? country = null,
        IEnumerable<Tag>? tags = null,
        DateTime? createdAtUtc = null)
    {
        if (yinaYinaId <= 0)
            return Result<Client>.Failure(ClientErrors.InvalidYinaYinaId());
        if (internalName.IsEmpty)
            return Result<Client>.Failure(ClientErrors.InternalNameRequired());

        var client = new Client(
            id,
            yinaYinaId,
            internalName,
            companyName,
            commercialName,
            primaryEmail,
            primaryPhone,
            addressLine1,
            addressLine2,
            city,
            postalCode,
            country,
            tags,
            createdAtUtc ?? DateTime.UtcNow);

        client.RaiseEvent(new ClientCreated(client.Id, client.YinaYinaId, client.InternalName, client.CreatedAt));
        return Result<Client>.Success(client);
    }

    public Result Rename(InternalName newName)
    {
        if (newName.IsEmpty)
            return Result.Failure(ClientErrors.InternalNameRequired());
        if (newName.Equals(InternalName)) return Result.Success();

        var old = InternalName;
        InternalName = newName;
        RaiseEvent(new ClientRenamed(Id, old, newName));
        return Result.Success();
    }

    public Result ChangePrimaryEmail(Email? email)
    {
        if (PrimaryEmail.Equals(email)) return Result.Success();

        var old = PrimaryEmail;
        PrimaryEmail = email;
        RaiseEvent(new ClientPrimaryEmailChanged(Id, old, email));
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
            case ClientCreated created:
                Id = created.ClientId;
                YinaYinaId = created.YinaYinaId;
                InternalName = created.InternalName;
                CreatedAt = created.CreatedAtUtc;
                break;
                
            case ClientRenamed renamed:
                InternalName = renamed.NewName;
                UpdatedAt = renamed.OccurredAtUtc;
                break;
                
            case ClientPrimaryEmailChanged emailChanged:
                PrimaryEmail = emailChanged.NewEmail;
                UpdatedAt = emailChanged.OccurredAtUtc;
                break;
                
            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }

    private static class ClientErrors
    {
        public static Error InvalidYinaYinaId() => Error.Create("CLIENT_YINAYINAID_INVALID", "YinaYinaId must be greater than zero", 400);
        public static Error InternalNameRequired() => Error.Create("CLIENT_INTERNALNAME_REQUIRED", "InternalName is required", 400);
    }
}


