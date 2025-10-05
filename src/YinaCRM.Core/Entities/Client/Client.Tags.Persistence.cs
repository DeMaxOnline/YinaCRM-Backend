using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using YinaCRM.Core.ValueObjects;

namespace YinaCRM.Core.Entities.Client;

public sealed partial class Client
{
    private readonly ObservableCollection<ClientTagRecord> _persistenceTags = new();
    private bool _persistenceInitialized;

    public ICollection<ClientTagRecord> PersistenceTags => _persistenceTags;

    private void EnsurePersistenceTagsInitialized()
    {
        if (_persistenceInitialized)
        {
            return;
        }

        _persistenceTags.CollectionChanged += OnPersistenceTagsChanged;
        _persistenceInitialized = true;
    }

    private void SyncPersistenceTagsFromDomain()
    {
        EnsurePersistenceTagsInitialized();

        _persistenceTags.CollectionChanged -= OnPersistenceTagsChanged;
        try
        {
            _persistenceTags.Clear();
            foreach (var tag in _tags)
            {
                _persistenceTags.Add(new ClientTagRecord
                {
                    Id = Guid.NewGuid(),
                    Value = tag.ToString()
                });
            }
        }
        finally
        {
            _persistenceTags.CollectionChanged += OnPersistenceTagsChanged;
        }
    }

    private void SyncDomainTagsFromPersistence()
    {
        EnsurePersistenceTagsInitialized();

        _tags.Clear();
        foreach (var record in _persistenceTags)
        {
            var result = Tag.TryCreate(record.Value);
            if (result.IsFailure)
            {
                throw new InvalidOperationException($"Invalid client tag in database: {record.Value}");
            }

            _tags.Add(result.Value);
        }
    }

    private void OnPersistenceTagsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SyncDomainTagsFromPersistence();

    public sealed class ClientTagRecord
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}

