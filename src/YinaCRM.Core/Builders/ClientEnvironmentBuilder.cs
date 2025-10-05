using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.ClientEnvironment;
using YinaCRM.Core.Entities.ClientEnvironment.VOs;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Identity.SecretVO;
using System.Linq;

namespace YinaCRM.Core.Builders;

/// <summary>
/// Fluent builder for ClientEnvironment aggregate. Accepts VOs or primitives converted via TryCreate.
/// Build() returns Result&lt;ClientEnvironment&gt;.
/// </summary>
public sealed class ClientEnvironmentBuilder
{
    private ClientEnvironmentId? _id;
    private ClientId? _clientId;
    private EnvironmentName? _name;
    private Description? _description;
    private Username? _username;
    private Secret? _password;
    private Body? _notes;
    private readonly List<EnvUrl> _urls = new();
    private DateTime? _createdAtUtc;
    private readonly List<Error> _errors = new();

    public ClientEnvironmentBuilder WithId(ClientEnvironmentId id) { _id = id; return this; }
    public ClientEnvironmentBuilder ForClient(ClientId clientId) { _clientId = clientId; return this; }

    public ClientEnvironmentBuilder WithName(EnvironmentName value) { _name = value; return this; }
    public ClientEnvironmentBuilder WithName(string value)
        => TryConvert(EnvironmentName.TryCreate(value), v => _name = v);

    public ClientEnvironmentBuilder WithDescription(Description value) { _description = value; return this; }
    public ClientEnvironmentBuilder WithDescription(string value)
        => TryConvert(Description.TryCreate(value), v => _description = v);

    public ClientEnvironmentBuilder WithUsername(Username value) { _username = value; return this; }
    public ClientEnvironmentBuilder WithUsername(string value)
        => TryConvert(Username.TryCreate(value), v => _username = v);

    public ClientEnvironmentBuilder WithPassword(Secret value) { _password = value; return this; }
    public ClientEnvironmentBuilder WithPassword(string value)
        => TryConvert(Secret.TryCreate(value), v => _password = v);

    public ClientEnvironmentBuilder WithNotes(Body value) { _notes = value; return this; }
    public ClientEnvironmentBuilder WithNotes(string value)
        => TryConvert(Body.TryCreate(value), v => _notes = v);

    public ClientEnvironmentBuilder AddUrl(EnvUrl envUrl) { _urls.Add(envUrl); return this; }
    public ClientEnvironmentBuilder AddUrl(UrlTypeCode type, Url url, bool isPrimary = false)
    {
        var r = EnvUrl.Create(type, url, isPrimary);
        if (r.IsSuccess) _urls.Add(r.Value);
        else _errors.Add(r.Error);
        return this;
    }
    public ClientEnvironmentBuilder AddUrl(string typeCode, string absoluteUrl, bool isPrimary = false)
    {
        var type = UrlTypeCode.TryCreate(typeCode);
        var url = Url.TryCreate(absoluteUrl);
        if (type.IsFailure) _errors.Add(type.Error);
        if (url.IsFailure) _errors.Add(url.Error);
        if (type.IsSuccess && url.IsSuccess)
        {
            var r = EnvUrl.Create(type.Value, url.Value, isPrimary);
            if (r.IsSuccess) _urls.Add(r.Value); else _errors.Add(r.Error);
        }
        return this;
    }

    public ClientEnvironmentBuilder WithCreatedAt(DateTime utc) { _createdAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc); return this; }

    public Result<ClientEnvironment> Build()
    {
        if (_errors.Count > 0)
            return Result<ClientEnvironment>.Failure(Error.Create("CLIENTENV_BUILDER_INVALID", string.Join("; ", _errors.Select(e => e.Message)), 400));

        if (_clientId is null)
            return Result<ClientEnvironment>.Failure(Error.Create("CLIENTENV_CLIENTID_REQUIRED", "ClientId must be provided", 400));
        if (_name is null)
            return Result<ClientEnvironment>.Failure(Error.Create("CLIENTENV_NAME_REQUIRED", "Environment name must be provided", 400));

        var id = _id ?? ClientEnvironmentId.New();
        return ClientEnvironment.Create(
            id,
            _clientId.Value,
            _name.Value,
            _description,
            _username,
            _password,
            _notes,
            _urls,
            _createdAtUtc);
    }

    private ClientEnvironmentBuilder TryConvert<T>(Result<T> result, Action<T> onSuccess)
    {
        if (result.IsSuccess) onSuccess(result.Value);
        else _errors.Add(result.Error);
        return this;
    }
}


