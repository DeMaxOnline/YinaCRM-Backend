using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using YinaCRM.Core.ValueObjects;

namespace YinaCRM.Core.Entities.Note;

public sealed partial class Note
{
    private readonly ObservableCollection<NoteTagRecord> _persistenceTags = new();
    private bool _persistenceInitialized;

    public ICollection<NoteTagRecord> PersistenceTags => _persistenceTags;

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
                _persistenceTags.Add(new NoteTagRecord
                {
                    Id = Guid.NewGuid(),
                    Value = tag.Value
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
                throw new InvalidOperationException($"Invalid note tag in database: {record.Value}");
            }

            _tags.Add(result.Value);
        }
    }

    private void OnPersistenceTagsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SyncDomainTagsFromPersistence();

    public sealed class NoteTagRecord
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}

