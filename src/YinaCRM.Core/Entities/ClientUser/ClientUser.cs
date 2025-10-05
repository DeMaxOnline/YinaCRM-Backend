using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.ClientUser.VOs;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
using YinaCRM.Core.ValueObjects.Identity.PhoneVO;

namespace YinaCRM.Core.Entities.ClientUser;

/// <summary>
/// External-facing user belonging to a Client.
/// </summary>
public sealed class ClientUser : AggregateRoot<ClientUserId>
{
    private ClientUser()
    {
        // Required by EF Core
    }

    private ClientUser(
        ClientUserId id,
        ClientId clientId,
        DisplayName displayName,
        Email? email,
        Phone? phone,
        RoleName? roleName,
        DateTime createdAt)
    {
        Id = id;
        ClientId = clientId;
        DisplayName = displayName;
        Email = email;
        Phone = phone;
        RoleName = roleName;
        CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
        UpdatedAt = null;
    }

    public override ClientUserId Id { get; protected set; }
    public ClientId ClientId { get; private set; }
    public DisplayName DisplayName { get; private set; }
    public Email? Email { get; private set; }
    public Phone? Phone { get; private set; }
    public RoleName? RoleName { get; private set; }

    public static Result<ClientUser> Create(
        ClientId clientId,
        DisplayName displayName,
        Email? email = null,
        Phone? phone = null,
        RoleName? roleName = null,
        DateTime? createdAtUtc = null)
        => Create(
            ClientUserId.New(),
            clientId,
            displayName,
            email,
            phone,
            roleName,
            createdAtUtc);

    public static Result<ClientUser> Create(
        ClientUserId id,
        ClientId clientId,
        DisplayName displayName,
        Email? email = null,
        Phone? phone = null,
        RoleName? roleName = null,
        DateTime? createdAtUtc = null)
    {
        if (displayName.IsEmpty) return Result<ClientUser>.Failure(Errors.DisplayNameRequired());
        var cu = new ClientUser(id, clientId, displayName, email, phone, roleName, createdAtUtc ?? DateTime.UtcNow);
        return Result<ClientUser>.Success(cu);
    }

    public Result Update(DisplayName displayName, Email? email, Phone? phone, RoleName? roleName)
    {
        if (displayName.IsEmpty) return Result.Failure(Errors.DisplayNameRequired());
        var changed = false;
        if (!DisplayName.Equals(displayName)) { DisplayName = displayName; changed = true; }
        if (!Equals(Email, email)) { Email = email; changed = true; }
        if (!Equals(Phone, phone)) { Phone = phone; changed = true; }
        if (!Equals(RoleName, roleName)) { RoleName = roleName; changed = true; }
        if (!changed) return Result.Success();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    private static class Errors
    {
        public static Error DisplayNameRequired() => Error.Create("CLIENTUSER_DISPLAYNAME_REQUIRED", "DisplayName is required", 400);
    }
}

// Tag type for ClientUserId strong ID generation
public readonly struct ClientUserIdTag { }


