using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.ClientEnvironment.Events;
using YinaCRM.Core.Entities.ClientEnvironment.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Identity.SecretVO;
using YinaCRM.Core.ValueObjects.Identity.UsernameVO;

namespace YinaCRM.Core.Entities.ClientEnvironment;

/// <summary>
/// Client environment aggregate with typed URLs and credentials/notes.
/// </summary>
public sealed partial class ClientEnvironment : AggregateRoot<ClientEnvironmentId>
{
    private readonly List<EnvUrl> _urls = new();

    private ClientEnvironment()
    {
        EnsurePersistenceUrlsInitialized();
    }

    private ClientEnvironment(
        ClientEnvironmentId id,
        ClientId clientId,
        EnvironmentName name,
        Description? description,
        Username? username,
        Secret? password,
        Body? notes,
        DateTime createdAt)
    {
        EnsurePersistenceUrlsInitialized();

        Id = id;
        ClientId = clientId;
        Name = name;
        Description = description;
        Username = username;
        Password = password;
        Notes = notes;
        CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
        UpdatedAt = null;

        SyncPersistenceUrlsFromDomain();
    }

    public override ClientEnvironmentId Id { get; protected set; }
    public ClientId ClientId { get; private set; }
    public EnvironmentName Name { get; private set; }
    public Description? Description { get; private set; }
    public IReadOnlyCollection<EnvUrl> Urls => _urls.AsReadOnly();
    public Username? Username { get; private set; }
    public Secret? Password { get; private set; }
    public Body? Notes { get; private set; }


    public static Result<ClientEnvironment> Create(
        ClientId clientId,
        EnvironmentName name,
        Description? description = null,
        Username? username = null,
        Secret? password = null,
        Body? notes = null,
        IEnumerable<EnvUrl>? urls = null,
        DateTime? createdAtUtc = null)
        => Create(
            ClientEnvironmentId.New(),
            clientId,
            name,
            description,
            username,
            password,
            notes,
            urls,
            createdAtUtc);

    public static Result<ClientEnvironment> Create(
        ClientEnvironmentId id,
        ClientId clientId,
        EnvironmentName name,
        Description? description = null,
        Username? username = null,
        Secret? password = null,
        Body? notes = null,
        IEnumerable<EnvUrl>? urls = null,
        DateTime? createdAtUtc = null)
    {
        if (name.IsEmpty)
            return Result<ClientEnvironment>.Failure(Errors.NameRequired());

        var list = urls?.ToList() ?? new List<EnvUrl>();
        if (!ValidatePrimaryPerType(list, out var code))
        {
            // code will have a value when validation fails
            return Result<ClientEnvironment>.Failure(Errors.PrimaryConflict(code.GetValueOrDefault()));
        }

        var env = new ClientEnvironment(
            id,
            clientId,
            name,
            description,
            username,
            password,
            notes,
            createdAtUtc ?? DateTime.UtcNow);

        if (list.Count > 0)
        {
            env.ReplaceUrls(list);
        }

        env.SyncPersistenceUrlsFromDomain();

        return Result<ClientEnvironment>.Success(env);
    }

    public Result AddUrl(UrlTypeCode typeCode, Url url, bool isPrimary = false)
    {
        var create = EnvUrl.Create(typeCode, url, isPrimary);
        if (create.IsFailure) return Result.Failure(create.Error);
        var envUrl = create.Value;

        if (isPrimary && _urls.Any(u => u.TypeCode.Equals(typeCode) && u.IsPrimary))
            return Result.Failure(Errors.PrimaryConflict(typeCode));

        _urls.Add(envUrl);
        SyncPersistenceUrlsFromDomain();
        RaiseEvent(new EnvironmentUrlAdded(Id, envUrl.Id, typeCode, url, isPrimary));
        return Result.Success();
    }

    public Result UpdateUrl(Guid id, UrlTypeCode typeCode, Url url, bool isPrimary)
    {
        var existingIndex = _urls.FindIndex(u => u.Id == id);
        if (existingIndex < 0) return Result.Failure(Errors.UrlNotFound());

        if (isPrimary && _urls.Any(u => u.Id != id && u.TypeCode.Equals(typeCode) && u.IsPrimary))
            return Result.Failure(Errors.PrimaryConflict(typeCode));

        var existing = _urls[existingIndex];
        var updateResult = existing.WithUpdates(typeCode, url, isPrimary);
        if (updateResult.IsFailure) return Result.Failure(updateResult.Error);

        _urls[existingIndex] = updateResult.Value;
        SyncPersistenceUrlsFromDomain();
        RaiseEvent(new EnvironmentUrlUpdated(Id, id, typeCode, url, isPrimary));
        return Result.Success();
    }

    public Result RemoveUrl(Guid id)
    {
        var idx = _urls.FindIndex(u => u.Id == id);
        if (idx < 0) return Result.Failure(Errors.UrlNotFound());

        _urls.RemoveAt(idx);
        SyncPersistenceUrlsFromDomain();
        RaiseEvent(new EnvironmentUrlRemoved(Id, id));
        return Result.Success();
    }

    public Result UpdateDetails(EnvironmentName name, Description? description, Username? username, Secret? password, Body? notes)
    {
        if (name.IsEmpty) return Result.Failure(Errors.NameRequired());
        Name = name;
        Description = description;
        Username = username;
        Password = password;
        Notes = notes;
        return Result.Success();
    }

    private void ReplaceUrls(IEnumerable<EnvUrl> urls)
    {
        _urls.Clear();
        _urls.AddRange(urls);
        SyncPersistenceUrlsFromDomain();
    }

    private static bool ValidatePrimaryPerType(IReadOnlyCollection<EnvUrl> urls, out UrlTypeCode? conflictType)
    {
        conflictType = default;
        var groups = urls.GroupBy(u => u.TypeCode);
        foreach (var g in groups)
        {
            if (g.Count(u => u.IsPrimary) > 1)
            {
                conflictType = g.Key;
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Applies events to rebuild the aggregate state during event sourcing replay.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    protected override void ApplyEvent(IDomainEvent @event)
    {
        switch (@event)
        {
            case EnvironmentUrlAdded added:
                var newUrl = EnvUrl.Create(added.UrlId, added.TypeCode, added.Url, added.IsPrimary);
                if (newUrl.IsSuccess)
                {
                    _urls.Add(newUrl.Value);
                }
                SyncPersistenceUrlsFromDomain();
                UpdatedAt = added.OccurredAtUtc;
                break;
                
            case EnvironmentUrlUpdated updated:
                var existingUrl = _urls.FirstOrDefault(u => u.Id == updated.UrlId);
                if (existingUrl != null)
                {
                    _urls.Remove(existingUrl);
                    var updatedUrl = EnvUrl.Create(updated.UrlId, updated.TypeCode, updated.Url, updated.IsPrimary);
                    if (updatedUrl.IsSuccess)
                    {
                        _urls.Add(updatedUrl.Value);
                    }
                }
                SyncPersistenceUrlsFromDomain();
                UpdatedAt = updated.OccurredAtUtc;
                break;
                
            case EnvironmentUrlRemoved removed:
                var urlToRemove = _urls.FirstOrDefault(u => u.Id == removed.UrlId);
                if (urlToRemove != null)
                {
                    _urls.Remove(urlToRemove);
                }
                SyncPersistenceUrlsFromDomain();
                UpdatedAt = removed.OccurredAtUtc;
                break;
                
            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }

    private static class Errors
    {
        public static Error NameRequired() => Error.Create("ENV_NAME_REQUIRED", "Environment name is required", 400);
        public static Error PrimaryConflict(UrlTypeCode code) => Error.Create("ENV_URL_PRIMARY_CONFLICT", $"Another primary URL already exists for type '{code}'", 409);
        public static Error UrlNotFound() => Error.Create("ENV_URL_NOT_FOUND", "Environment URL not found", 404);
    }
}








