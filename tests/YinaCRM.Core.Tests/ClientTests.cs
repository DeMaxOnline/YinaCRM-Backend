using System;
using System.Linq;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.Client;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using YinaCRM.Core.Entities.Client.Events;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;

namespace YinaCRM.Core.Tests;

public sealed class ClientTests
{
    [Fact]
    public void Create_WithValidData_InitialisesStateAndRaisesEvent()
    {
        var tags = new[] { DomainTestHelper.Tag("vip"), DomainTestHelper.Tag("beta") };

        var result = Client.Create(
            42,
            DomainTestHelper.InternalName(),
            DomainTestHelper.CompanyName(),
            DomainTestHelper.CommercialName(),
            DomainTestHelper.Email(),
            DomainTestHelper.Phone(),
            DomainTestHelper.AddressLine(),
            null,
            DomainTestHelper.City(),
            DomainTestHelper.PostalCode(),
            DomainTestHelper.Country(),
            tags);

        Assert.True(result.IsSuccess);
        var client = result.Value;

        Assert.Equal(42, client.YinaYinaId);
        Assert.Equal("acme-co", client.InternalName.ToString());
        Assert.Equal(2, client.Tags.Count);
        Assert.Equal(2, client.PersistenceTags.Count);

        // Roundtrip persistence collection into domain
        client.PersistenceTags.Add(new Client.ClientTagRecord { Id = Guid.NewGuid(), Value = "new" });
        Assert.Equal(3, client.Tags.Count);

        var events = client.DequeueEvents();
        var created = Assert.IsType<ClientCreated>(Assert.Single(events));
        Assert.Equal(client.Id, created.ClientId);
    }

    [Fact]
    public void RenameAndEmailChanges_RespectInvariants()
    {
        var client = DomainTestHelper.ExpectValue(Client.Create(
            100,
            DomainTestHelper.InternalName(),
            tags: new[] { DomainTestHelper.Tag() }));
        client.DequeueEvents();

        var renameResult = client.Rename(DomainTestHelper.InternalName("new-name"));
        Assert.True(renameResult.IsSuccess);
        var renameEvent = Assert.IsType<ClientRenamed>(client.DequeueEvents().Single());
        Assert.Equal("new-name", renameEvent.NewName.ToString());

        // Same value results in no extra event
        var noRename = client.Rename(DomainTestHelper.InternalName("new-name"));
        Assert.True(noRename.IsSuccess);
        Assert.Empty(client.DequeueEvents());

        var email = DomainTestHelper.Email("owner@example.com");
        Assert.True(client.ChangePrimaryEmail(email).IsSuccess);
        var emailChanged = Assert.IsType<ClientPrimaryEmailChanged>(client.DequeueEvents().Single());
        Assert.Equal(email, emailChanged.NewEmail);

        // Setting to same email yields no additional event
        Assert.True(client.ChangePrimaryEmail(email).IsSuccess);
        Assert.Empty(client.DequeueEvents());
    }

    [Fact]
    public void Create_WithInvalidId_Fails()
    {
        var result = Client.Create(
            ClientId.New(),
            0,
            DomainTestHelper.InternalName(),
            DomainTestHelper.CompanyName());

        Assert.True(result.IsFailure);
        Assert.Equal("CLIENT_YINAYINAID_INVALID", result.Error.Code);
    }
}




