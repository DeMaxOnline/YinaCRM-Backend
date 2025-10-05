using System;
using System.Linq;
using ActorKindCode = YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO.ActorKindCode;
using YinaCRM.Core.Entities.Note;
using YinaCRM.Core.Entities.Note.Events;

namespace YinaCRM.Core.Tests;

public sealed class NoteTests
{
    [Fact]
    public void CreateEditPinAndUnpinNote()
    {
        var authorKind = DomainTestHelper.ActorKind("User");
        var note = DomainTestHelper.ExpectValue(Note.Create(
            DomainTestHelper.Body("Initial contents"),
            DomainTestHelper.Visibility("internal"),
            pinned: false,
            tags: DomainTestHelper.Tags("alpha", "beta"),
            createdByKind: authorKind,
            createdById: Guid.NewGuid()));

        var created = Assert.IsType<NoteCreated>(note.DequeueEvents().Single());
        Assert.Equal(authorKind.ToString(), created.CreatedByKind.ToString());

        Assert.True(note.Edit(DomainTestHelper.Body("Edited")).IsSuccess);
        Assert.IsType<NoteEdited>(note.DequeueEvents().Single());

        Assert.True(note.Pin().IsSuccess);
        Assert.IsType<NotePinned>(note.DequeueEvents().Single());

        // Re-pin is no-op
        Assert.True(note.Pin().IsSuccess);
        Assert.Empty(note.DequeueEvents());

        Assert.True(note.Unpin().IsSuccess);
        Assert.IsType<NoteUnpinned>(note.DequeueEvents().Single());
    }

    [Fact]
    public void ClientUserCannotCreateInternalNote()
    {
        var clientUserKind = DomainTestHelper.ActorKind("ClientUser");
        var failure = Note.Create(
            DomainTestHelper.Body(),
            DomainTestHelper.Visibility("internal"),
            pinned: false,
            tags: null,
            createdByKind: clientUserKind,
            createdById: Guid.NewGuid());
        Assert.True(failure.IsFailure);
        Assert.Equal("NOTE_INTERNAL_FOR_CLIENTUSER", failure.Error.Code);
    }
}

