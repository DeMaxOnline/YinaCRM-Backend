// Placeholder VO: Country (composite)
#nullable enable
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Code;
using YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name;

namespace YinaCRM.Core.ValueObjects.AddressVO.CountryVO;

/// <summary>
/// Country value object composed of <see cref="CountryCode"/> and <see cref="CountryName"/>.
/// </summary>
public readonly record struct Country
{
    public CountryCode Code { get; }
    public CountryName Name { get; }

    private Country(CountryCode code, CountryName name)
    {
        Code = code;
        Name = name;
    }

    public override string ToString() => $"{Name} ({Code})";

    public static Result<Country> TryCreate(string? code, string? name)
    {
        var cc = CountryCode.TryCreate(code);
        if (cc.IsFailure) return Result<Country>.Failure(cc.Error);

        var cn = CountryName.TryCreate(name);
        if (cn.IsFailure) return Result<Country>.Failure(cn.Error);

        return Result<Country>.Success(new Country(cc.Value, cn.Value));
    }
}


