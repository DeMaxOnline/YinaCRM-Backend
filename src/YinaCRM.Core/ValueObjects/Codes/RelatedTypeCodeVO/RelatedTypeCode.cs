// Placeholder VO: RelatedTypeCode (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects.Codes.RelatedTypeCodeVO;

/// <summary>
/// Cross-cutting related type code.
/// Allowed values: Client, SupportTicket, Hardware, ClientEnvironment, ModuleSubscription.
/// Normalization: trims and case-insensitive matching to canonical value.
/// </summary>
public readonly record struct RelatedTypeCode
{
    private static readonly string[] Allowed = new[]
    {
        "Client", "SupportTicket", "Hardware", "ClientEnvironment", "ModuleSubscription"
    };

    internal string Value { get; }
    private RelatedTypeCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<RelatedTypeCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<RelatedTypeCode>.Failure(RelatedTypeCodeErrors.Empty());
        var s = input.Trim();
        foreach (var a in Allowed)
        {
            if (string.Equals(a, s, StringComparison.OrdinalIgnoreCase))
                return Result<RelatedTypeCode>.Success(new RelatedTypeCode(a));
        }
        return Result<RelatedTypeCode>.Failure(RelatedTypeCodeErrors.Invalid());
    }
}


