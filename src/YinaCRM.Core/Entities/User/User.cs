using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.User.VOs;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Identity.AuthSubjectVO;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
namespace YinaCRM.Core.Entities.User;
/// <summary>
/// Internal user entity (no persistence concerns). Uses typed VOs.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private User()
    {
        // Required by EF Core
    }
    private User(
        UserId id,
        AuthSubject authSub,
        DisplayName displayName,
        Email email,
        TimeZoneId? timeZone,
        LocaleCode? locale,
        DateTime createdAtUtc)
    {
        Id = id;
        AuthSub = authSub;
        DisplayName = displayName;
        Email = email;
        TimeZone = timeZone;
        Locale = locale;
        CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        UpdatedAt = null;
    }
    public override UserId Id { get; protected set; }
    public AuthSubject AuthSub { get; private set; }
    public DisplayName DisplayName { get; private set; }
    public Email Email { get; private set; }
    public TimeZoneId? TimeZone { get; private set; }
    public LocaleCode? Locale { get; private set; }
    public static Result<User> Create(
        AuthSubject authSub,
        DisplayName displayName,
        Email email,
        TimeZoneId? timeZone = null,
        LocaleCode? locale = null,
        DateTime? createdAtUtc = null)
        => Create(
            UserId.New(),
            authSub,
            displayName,
            email,
            timeZone,
            locale,
            createdAtUtc);
    public static Result<User> Create(
        UserId id,
        AuthSubject authSub,
        DisplayName displayName,
        Email email,
        TimeZoneId? timeZone = null,
        LocaleCode? locale = null,
        DateTime? createdAtUtc = null)
    {
        if (authSub.IsEmpty) return Result<User>.Failure(Errors.AuthSubRequired());
        if (displayName.IsEmpty) return Result<User>.Failure(Errors.DisplayNameRequired());
        var user = new User(id, authSub, displayName, email, timeZone, locale, createdAtUtc ?? DateTime.UtcNow);
        return Result<User>.Success(user);
    }
    public Result UpdateProfile(DisplayName displayName, Email email, TimeZoneId? timeZone, LocaleCode? locale)
    {
        if (displayName.IsEmpty) return Result.Failure(Errors.DisplayNameRequired());
        var changed = false;
        if (!DisplayName.Equals(displayName)) { DisplayName = displayName; changed = true; }
        if (!Email.Equals(email)) { Email = email; changed = true; }
        if (!Equals(TimeZone, timeZone)) { TimeZone = timeZone; changed = true; }
        if (!Equals(Locale, locale)) { Locale = locale; changed = true; }
        if (!changed) return Result.Success();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
    private static class Errors
    {
        public static Error AuthSubRequired() => Error.Create("USER_AUTHSUB_REQUIRED", "AuthSub is required", 400);
        public static Error DisplayNameRequired() => Error.Create("USER_DISPLAYNAME_REQUIRED", "DisplayName is required", 400);
    }
}
// Tag type for UserId strong ID generation
public readonly struct UserIdTag { }



