// Placeholder VO: Contact (composite)
#nullable enable
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
using YinaCRM.Core.ValueObjects.Identity.PhoneVO;

namespace YinaCRM.Core.ValueObjects.Identity.ContactVO;

/// <summary>
/// Contact value object composed of Email and Phone.
/// At least one of Email or Phone must be provided.
/// </summary>
public readonly record struct Contact
{
    public Email? Email { get; }
    public Phone? Phone { get; }

    private Contact(Email? email, Phone? phone)
    {
        Email = email;
        Phone = phone;
    }

    public override string ToString()
        => Email is { } e && Phone is { } p ? $"{e}, {p}" : Email?.ToString() ?? Phone?.ToString() ?? string.Empty;

    public static Result<Contact> TryCreate(string? email, string? phone)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPhone = !string.IsNullOrWhiteSpace(phone);
        if (!hasEmail && !hasPhone)
            return Result<Contact>.Failure(ContactErrors.Empty());

        Email? e = null;
        Phone? p = null;

        if (hasEmail)
        {
            var er = EmailVO.Email.TryCreate(email);
            if (er.IsFailure) return Result<Contact>.Failure(er.Error);
            e = er.Value;
        }

        if (hasPhone)
        {
            var pr = PhoneVO.Phone.TryCreate(phone);
            if (pr.IsFailure) return Result<Contact>.Failure(pr.Error);
            p = pr.Value;
        }

        return Result<Contact>.Success(new Contact(e, p));
    }
}

