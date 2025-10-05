// Placeholder VO: Address (composite)
#nullable enable
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;
using YinaCRM.Core.ValueObjects.AddressVO.CityVO;
using YinaCRM.Core.ValueObjects.AddressVO.CountryVO;
using YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;
using YinaCRM.Core.ValueObjects.AddressVO.RegionVO;

namespace YinaCRM.Core.ValueObjects.AddressVO;

/// <summary>
/// Address value object composed of AddressLine, City, Region, PostalCode, Country.
/// </summary>
public readonly record struct Address
{
    public AddressLine Line { get; }
    public City City { get; }
    public Region Region { get; }
    public PostalCode PostalCode { get; }
    public Country Country { get; }

    private Address(AddressLine line, City city, Region region, PostalCode postal, Country country)
    {
        Line = line;
        City = city;
        Region = region;
        PostalCode = postal;
        Country = country;
    }

    public override string ToString() => $"{Line}, {City}, {Region} {PostalCode}, {Country}";

    public static Result<Address> TryCreate(
        string? line,
        string? city,
        string? regionName,
        string? regionIsoCode,
        string? postalCode,
        string? countryCode,
        string? countryName)
    {
        var l = AddressLine.TryCreate(line);
        if (l.IsFailure) return Result<Address>.Failure(l.Error);

        var c = City.TryCreate(city);
        if (c.IsFailure) return Result<Address>.Failure(c.Error);

        var r = Region.TryCreate(regionName, regionIsoCode);
        if (r.IsFailure) return Result<Address>.Failure(r.Error);

        var p = PostalCode.TryCreate(postalCode);
        if (p.IsFailure) return Result<Address>.Failure(p.Error);

        var co = Country.TryCreate(countryCode, countryName);
        if (co.IsFailure) return Result<Address>.Failure(co.Error);

        return Result<Address>.Success(new Address(l.Value, c.Value, r.Value, p.Value, co.Value));
    }
}


