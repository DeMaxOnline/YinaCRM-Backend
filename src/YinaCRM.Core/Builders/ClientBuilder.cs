using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.Client;
using YinaCRM.Core.Entities.Client.VOs;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;
using YinaCRM.Core.ValueObjects.AddressVO.CityVO;
using YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name;
using YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
using YinaCRM.Core.ValueObjects.Identity.PhoneVO;

namespace YinaCRM.Core.Builders;

/// <summary>
/// Fluent builder for Client aggregate. Accepts VOs or primitives that are immediately
/// validated via VO.TryCreate. Build() returns Result&lt;Client&gt; and, on success, the
/// Client factory raises ClientCreated.
/// </summary>
public sealed class ClientBuilder
{
    private ClientId? _id;
    private int? _yinaYinaId;
    private InternalName? _internalName;
    private CompanyName? _companyName;
    private CommercialName? _commercialName;
    private Email? _primaryEmail;
    private Phone? _primaryPhone;
    private AddressLine? _addressLine1;
    private AddressLine? _addressLine2;
    private City? _city;
    private PostalCode? _postalCode;
    private CountryName? _country;
    private readonly List<Tag> _tags = new();
    private DateTime? _createdAtUtc;
    private readonly List<Error> _errors = new();

    public ClientBuilder WithId(ClientId id) { _id = id; return this; }
    public ClientBuilder WithYinaYinaId(int value) { _yinaYinaId = value; return this; }

    public ClientBuilder WithInternalName(InternalName value) { _internalName = value; return this; }
    public ClientBuilder WithInternalName(string value)
        => TryConvert(InternalName.TryCreate(value), v => _internalName = v);

    public ClientBuilder WithCompanyName(CompanyName value) { _companyName = value; return this; }
    public ClientBuilder WithCompanyName(string value)
        => TryConvert(CompanyName.TryCreate(value), v => _companyName = v);

    public ClientBuilder WithCommercialName(CommercialName value) { _commercialName = value; return this; }
    public ClientBuilder WithCommercialName(string value)
        => TryConvert(CommercialName.TryCreate(value), v => _commercialName = v);

    public ClientBuilder WithPrimaryEmail(Email value) { _primaryEmail = value; return this; }
    public ClientBuilder WithPrimaryEmail(string value)
        => TryConvert(Email.TryCreate(value), v => _primaryEmail = v);

    public ClientBuilder WithPrimaryPhone(Phone value) { _primaryPhone = value; return this; }
    public ClientBuilder WithPrimaryPhone(string value)
        => TryConvert(Phone.TryCreate(value), v => _primaryPhone = v);

    public ClientBuilder WithAddressLine1(AddressLine value) { _addressLine1 = value; return this; }
    public ClientBuilder WithAddressLine1(string value)
        => TryConvert(AddressLine.TryCreate(value), v => _addressLine1 = v);

    public ClientBuilder WithAddressLine2(AddressLine value) { _addressLine2 = value; return this; }
    public ClientBuilder WithAddressLine2(string value)
        => TryConvert(AddressLine.TryCreate(value), v => _addressLine2 = v);

    public ClientBuilder WithCity(City value) { _city = value; return this; }
    public ClientBuilder WithCity(string value)
        => TryConvert(City.TryCreate(value), v => _city = v);

    public ClientBuilder WithPostalCode(PostalCode value) { _postalCode = value; return this; }
    public ClientBuilder WithPostalCode(string value)
        => TryConvert(PostalCode.TryCreate(value), v => _postalCode = v);

    public ClientBuilder WithCountry(CountryName value) { _country = value; return this; }
    public ClientBuilder WithCountry(string value)
        => TryConvert(CountryName.TryCreate(value), v => _country = v);

    public ClientBuilder AddTag(Tag tag) { _tags.Add(tag); return this; }
    public ClientBuilder AddTag(string value)
        => TryConvert(Tag.TryCreate(value), v => _tags.Add(v));

    public ClientBuilder WithCreatedAt(DateTime utc) { _createdAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc); return this; }

    public Result<Client> Build()
    {
        if (_errors.Count > 0)
            return Result<Client>.Failure(Error.Create("CLIENT_BUILDER_INVALID", string.Join("; ", _errors.Select(e => e.Message)), 400));

        var id = _id ?? ClientId.New();
        var yid = _yinaYinaId ?? 0;
        if (_internalName is null)
            return Result<Client>.Failure(Error.Create("CLIENT_INTERNALNAME_REQUIRED", "InternalName must be provided", 400));

        return Client.Create(
            id,
            yid,
            _internalName.Value,
            _companyName,
            _commercialName,
            _primaryEmail,
            _primaryPhone,
            _addressLine1,
            _addressLine2,
            _city,
            _postalCode,
            _country,
            _tags,
            _createdAtUtc);
    }

    private ClientBuilder TryConvert<T>(Result<T> result, Action<T> onSuccess)
    {
        if (result.IsSuccess) onSuccess(result.Value);
        else _errors.Add(result.Error);
        return this;
    }
}


