using System;
using System.Linq;
using InteractionId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.InteractionIdTag>;
using YinaCRM.Core.Entities.Interaction;
using YinaCRM.Core.Entities.Interaction.Events;

namespace YinaCRM.Core.Tests;

public sealed class InteractionTests
{
    [Fact]
    public void CreateAndMutateInteraction()
    {
        var createdById = Guid.NewGuid();
        var interaction = DomainTestHelper.ExpectValue(Interaction.Create(
            DomainTestHelper.InteractionType(),
            DomainTestHelper.InteractionDirection(),
            DomainTestHelper.Title(),
            DomainTestHelper.Body(),
            scheduledAt: DateTime.UtcNow.AddHours(1),
            duration: TimeSpan.FromMinutes(30),
            createdByKind: DomainTestHelper.ActorKind("User"),
            createdById: createdById));

        var createdEvent = Assert.IsType<InteractionCreated>(interaction.DequeueEvents().Single());
        Assert.Equal(createdById, createdEvent.CreatedById);

        var participantId = Guid.NewGuid();
        Assert.True(interaction.AddParticipant(DomainTestHelper.ActorKind("User"), participantId, DomainTestHelper.ParticipantRole("organizer")).IsSuccess);
        var participantAdded = Assert.IsType<InteractionParticipantAdded>(interaction.DequeueEvents().Single());
        Assert.Equal(participantId, participantAdded.ParticipantId);

        var duplicate = interaction.AddParticipant(DomainTestHelper.ActorKind("User"), participantId, DomainTestHelper.ParticipantRole("organizer"));
        Assert.True(duplicate.IsFailure);
        Assert.Equal("INTERACTION_PARTICIPANT_EXISTS", duplicate.Error.Code);

        Assert.True(interaction.RemoveParticipant(DomainTestHelper.ActorKind("User"), participantId).IsSuccess);
        Assert.IsType<InteractionParticipantRemoved>(interaction.DequeueEvents().Single());

        var missingRemoval = interaction.RemoveParticipant(DomainTestHelper.ActorKind("User"), participantId);
        Assert.True(missingRemoval.IsFailure);
        Assert.Equal("INTERACTION_PARTICIPANT_NOT_FOUND", missingRemoval.Error.Code);

        var relatedId = Guid.NewGuid();
        Assert.True(interaction.AddLink("SupportTicket", relatedId).IsSuccess);
        Assert.True(interaction.AddLink("SupportTicket", relatedId).IsFailure);
        Assert.True(interaction.RemoveLink("SupportTicket", relatedId).IsSuccess);
        Assert.True(interaction.RemoveLink("SupportTicket", relatedId).IsFailure);

        var updateResult = interaction.UpdateDetails(
            DomainTestHelper.Title("Updated subject"),
            DomainTestHelper.Body("New description"),
            DateTime.UtcNow.AddHours(2));
        Assert.True(updateResult.IsSuccess);
        var updated = Assert.IsType<InteractionUpdated>(interaction.DequeueEvents().Single());
        Assert.Equal("Updated subject", updated.UpdatedSubject);

        Assert.True(interaction.Complete(DateTime.UtcNow, TimeSpan.FromMinutes(15)).IsSuccess);
        Assert.NotNull(interaction.CompletedAt);
        Assert.Equal(TimeSpan.FromMinutes(15), interaction.Duration);

        interaction.PersistenceParticipants.Add(new Interaction.InteractionParticipantRecord
        {
            ParticipantKind = "User",
            ParticipantId = Guid.NewGuid(),
            Role = "attendee"
        });
        Assert.Single(interaction.Participants);

        interaction.PersistenceLinks.Add(new Interaction.InteractionLinkRecord
        {
            RelatedType = "Note",
            RelatedId = Guid.NewGuid()
        });
        Assert.Single(interaction.Links);
    }

    [Fact]
    public void UpdateDetails_AllowsNullsWithoutEvents()
    {
        var interaction = DomainTestHelper.ExpectValue(Interaction.Create(
            DomainTestHelper.InteractionType(),
            DomainTestHelper.InteractionDirection(),
            DomainTestHelper.Title()));

        interaction.DequeueEvents();

        var result = interaction.UpdateDetails(null, null, null);
        Assert.True(result.IsSuccess);
        Assert.Empty(interaction.DequeueEvents());
    }

    [Fact]
    public void EventReplay_RebuildsState()
    {
        var interaction = DomainTestHelper.ExpectValue(Interaction.Create(
            DomainTestHelper.InteractionType(),
            DomainTestHelper.InteractionDirection(),
            DomainTestHelper.Title("Initial"),
            DomainTestHelper.Body("Body")));

        var participantId = Guid.NewGuid();
        interaction.AddParticipant(DomainTestHelper.ActorKind("User"), participantId, DomainTestHelper.ParticipantRole("organizer"));
        interaction.UpdateDetails(DomainTestHelper.Title("Follow-up"), DomainTestHelper.Body("Discussed upgrades"), DateTime.UtcNow.AddHours(1));

        var events = interaction.DequeueEvents().ToArray();
        var rehydrated = (Interaction)Activator.CreateInstance(typeof(Interaction), nonPublic: true)!;
        rehydrated.LoadFromHistory(events);

        Assert.Equal(interaction.Subject.ToString(), rehydrated.Subject.ToString());
        Assert.Equal(interaction.Description?.ToString(), rehydrated.Description?.ToString());
        var expectedParticipantId = interaction.Participants.First().ParticipantId;
        var rehydratedParticipantId = rehydrated.Participants.Single().ParticipantId;
        Assert.Equal(expectedParticipantId, rehydratedParticipantId);
    }

    [Fact]
    public void PersistenceParticipantRecord_InvalidParticipant_Throws()
    {
        var interaction = DomainTestHelper.ExpectValue(Interaction.Create(
            DomainTestHelper.InteractionType(),
            DomainTestHelper.InteractionDirection(),
            DomainTestHelper.Title()));

        Assert.Throws<InvalidOperationException>(() => interaction.PersistenceParticipants.Add(new Interaction.InteractionParticipantRecord
        {
            ParticipantKind = "Invalid",
            ParticipantId = Guid.NewGuid(),
            Role = "organizer"
        }));
    }
}
