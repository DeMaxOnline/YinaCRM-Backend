// Placeholder VO: TimeZoneId (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Time zone ID value object (system time zone identifier).
/// Normalization: trims; uses the canonical system ID casing if found.
/// Validation: must exist in <see cref="TimeZoneInfo.GetSystemTimeZones"/>.
/// </summary>
public readonly record struct TimeZoneId
{
    internal string Value { get; }
    private TimeZoneId(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<TimeZoneId> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<TimeZoneId>.Failure(TimeZoneIdErrors.Empty());
        var s = input.Trim();

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(s);
            return Result<TimeZoneId>.Success(new TimeZoneId(tz.Id));
        }
        catch
        {
            foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
            {
                if (string.Equals(tz.Id, s, StringComparison.OrdinalIgnoreCase))
                    return Result<TimeZoneId>.Success(new TimeZoneId(tz.Id));
            }
            return Result<TimeZoneId>.Failure(TimeZoneIdErrors.Invalid());
        }
    }
}


