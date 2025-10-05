using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using YinaCRM.Core.ValueObjects;

namespace YinaCRM.Core.Entities.ClientEnvironment;

public sealed partial class ClientEnvironment
{
    private readonly ObservableCollection<ClientEnvironmentUrlRecord> _persistenceUrls = new();
    private bool _persistenceUrlsInitialized;

    public ICollection<ClientEnvironmentUrlRecord> PersistenceUrls => _persistenceUrls;

    private void EnsurePersistenceUrlsInitialized()
    {
        if (_persistenceUrlsInitialized)
        {
            return;
        }

        _persistenceUrls.CollectionChanged += OnPersistenceUrlsChanged;
        _persistenceUrlsInitialized = true;
    }

    private void SyncPersistenceUrlsFromDomain()
    {
        EnsurePersistenceUrlsInitialized();

        _persistenceUrls.CollectionChanged -= OnPersistenceUrlsChanged;
        try
        {
            _persistenceUrls.Clear();
            foreach (var url in _urls)
            {
                _persistenceUrls.Add(new ClientEnvironmentUrlRecord
                {
                    Id = url.Id,
                    TypeCode = url.TypeCode.ToString(),
                    Url = url.Url.ToString(),
                    IsPrimary = url.IsPrimary
                });
            }
        }
        finally
        {
            _persistenceUrls.CollectionChanged += OnPersistenceUrlsChanged;
        }
    }

    private void SyncDomainUrlsFromPersistence()
    {
        EnsurePersistenceUrlsInitialized();

        _urls.Clear();
        foreach (var record in _persistenceUrls)
        {
            var typeResult = UrlTypeCode.TryCreate(record.TypeCode);
            if (typeResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid client environment URL type in database: {record.TypeCode}");
            }

            var urlResult = Url.TryCreate(record.Url);
            if (urlResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid client environment URL in database: {record.Url}");
            }

            var envUrlResult = EnvUrl.Create(
                record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
                typeResult.Value,
                urlResult.Value,
                record.IsPrimary);

            if (envUrlResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid client environment URL record: {envUrlResult.Error.Message}");
            }

            _urls.Add(envUrlResult.Value);
        }
    }

    private void OnPersistenceUrlsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SyncDomainUrlsFromPersistence();

    public sealed class ClientEnvironmentUrlRecord
    {
        public Guid Id { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}

