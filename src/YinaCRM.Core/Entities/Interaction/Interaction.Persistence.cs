using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Interaction;

public sealed partial class Interaction
{
    private readonly ObservableCollection<InteractionParticipantRecord> _persistenceParticipants = new();
    private readonly ObservableCollection<InteractionLinkRecord> _persistenceLinks = new();
    private bool _persistenceInitialized;

    public ICollection<InteractionParticipantRecord> PersistenceParticipants => _persistenceParticipants;
    public ICollection<InteractionLinkRecord> PersistenceLinks => _persistenceLinks;

    private void EnsurePersistenceCollectionsInitialized()
    {
        if (_persistenceInitialized)
        {
            return;
        }

        _persistenceParticipants.CollectionChanged += OnPersistenceParticipantsChanged;
        _persistenceLinks.CollectionChanged += OnPersistenceLinksChanged;
        _persistenceInitialized = true;
    }

    private void SyncPersistenceCollectionsFromDomain()
    {
        SyncPersistenceParticipantsFromDomain();
        SyncPersistenceLinksFromDomain();
    }

    private void SyncPersistenceParticipantsFromDomain()
    {
        EnsurePersistenceCollectionsInitialized();

        _persistenceParticipants.CollectionChanged -= OnPersistenceParticipantsChanged;
        try
        {
            _persistenceParticipants.Clear();
            foreach (var participant in _participants)
            {
                _persistenceParticipants.Add(new InteractionParticipantRecord
                {
                    ParticipantKind = participant.ParticipantKind.ToString(),
                    ParticipantId = participant.ParticipantId,
                    Role = participant.Role.ToString()
                });
            }
        }
        finally
        {
            _persistenceParticipants.CollectionChanged += OnPersistenceParticipantsChanged;
        }
    }

    private void SyncPersistenceLinksFromDomain()
    {
        EnsurePersistenceCollectionsInitialized();

        _persistenceLinks.CollectionChanged -= OnPersistenceLinksChanged;
        try
        {
            _persistenceLinks.Clear();
            foreach (var link in _links)
            {
                _persistenceLinks.Add(new InteractionLinkRecord
                {
                    RelatedType = link.RelatedType,
                    RelatedId = link.RelatedId
                });
            }
        }
        finally
        {
            _persistenceLinks.CollectionChanged += OnPersistenceLinksChanged;
        }
    }

    private void SyncDomainParticipantsFromPersistence()
    {
        EnsurePersistenceCollectionsInitialized();

        _participants.Clear();
        foreach (var record in _persistenceParticipants)
        {
            var kindResult = ActorKindCode.TryCreate(record.ParticipantKind);
            if (kindResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid interaction participant kind in database: {record.ParticipantKind}");
            }

            var roleResult = ParticipantRoleCode.TryCreate(record.Role);
            if (roleResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid interaction participant role in database: {record.Role}");
            }

            var participantResult = InteractionParticipant.Create(
                Id,
                kindResult.Value,
                record.ParticipantId,
                roleResult.Value);

            if (participantResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid interaction participant record: {participantResult.Error.Message}");
            }

            _participants.Add(participantResult.Value);
        }
    }

    private void SyncDomainLinksFromPersistence()
    {
        EnsurePersistenceCollectionsInitialized();

        _links.Clear();
        foreach (var record in _persistenceLinks)
        {
            var linkResult = InteractionLink.Create(Id, record.RelatedType, record.RelatedId);
            if (linkResult.IsFailure)
            {
                throw new InvalidOperationException($"Invalid interaction link record: {linkResult.Error.Message}");
            }

            _links.Add(linkResult.Value);
        }
    }

    private void OnPersistenceParticipantsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SyncDomainParticipantsFromPersistence();

    private void OnPersistenceLinksChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SyncDomainLinksFromPersistence();

    public sealed class InteractionParticipantRecord
    {
        public string ParticipantKind { get; set; } = string.Empty;
        public Guid ParticipantId { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public sealed class InteractionLinkRecord
    {
        public string RelatedType { get; set; } = string.Empty;
        public Guid RelatedId { get; set; }
    }
}


