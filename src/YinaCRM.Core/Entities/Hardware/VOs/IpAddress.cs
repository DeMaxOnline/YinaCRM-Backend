// VO: IpAddress (optional, local)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

/// <summary>
/// Represents an IP address or normalized textual host-like value.
/// Accepts IPv4/IPv6 patterns or a lowercase host token (a-z, 0-9, '.', '-').
/// Pure domain validation without infrastructure dependencies.
/// </summary>
public readonly partial record struct IpAddress
{
    internal string Value { get; }
    private IpAddress(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<IpAddress> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<IpAddress>.Failure(IpAddressErrors.Empty());

        var s = input.Trim();

        // Check for IPv4 pattern (simplified but effective)
        if (IPv4Pattern().IsMatch(s))
        {
            // Validate octets are in range 0-255
            var parts = s.Split('.');
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var octet) && (octet < 0 || octet > 255))
                    return Result<IpAddress>.Failure(IpAddressErrors.Invalid());
            }
            return Result<IpAddress>.Success(new IpAddress(s));
        }

        // Check for IPv6 pattern (simplified)
        if (IPv6Pattern().IsMatch(s))
        {
            var normalized = s.ToLowerInvariant();
            return Result<IpAddress>.Success(new IpAddress(normalized));
        }

        // Fall back to host-like normalized text
        var host = s.ToLowerInvariant();
        if (!HostPattern().IsMatch(host) || host.Length > 254)
            return Result<IpAddress>.Failure(IpAddressErrors.Invalid());

        return Result<IpAddress>.Success(new IpAddress(host));
    }

    [GeneratedRegex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled)]
    private static partial Regex IPv4Pattern();

    [GeneratedRegex(@"^(([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))$", RegexOptions.Compiled)]
    private static partial Regex IPv6Pattern();

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)*$", RegexOptions.Compiled)]
    private static partial Regex HostPattern();
}

public static class IpAddressErrors
{
    public static Error Empty() => Error.Create("HW_IP_EMPTY", "IP/host is required when provided", 400);
    public static Error Invalid() => Error.Create("HW_IP_INVALID", "Value must be a valid IPv4/IPv6 or host name", 400);
}



