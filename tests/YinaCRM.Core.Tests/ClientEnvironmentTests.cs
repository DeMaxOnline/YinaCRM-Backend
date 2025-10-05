using System;
using System.Linq;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using ClientEnvironmentId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.ClientEnvironment.ClientEnvironmentIdTag>;
using YinaCRM.Core.Entities.ClientEnvironment;
using YinaCRM.Core.Entities.ClientEnvironment.Events;

namespace YinaCRM.Core.Tests;

public sealed class ClientEnvironmentTests
{
    [Fact]
    public void CreateAndManageUrls_WorksAsExpected()
    {
        var clientId = ClientId.New();
        var primaryUrl = DomainTestHelper.ExpectValue(EnvUrl.Create(DomainTestHelper.UrlType("portal"), DomainTestHelper.Url("https://portal.example.com"), true));
        var secondaryUrl = DomainTestHelper.ExpectValue(EnvUrl.Create(DomainTestHelper.UrlType("docs"), DomainTestHelper.Url("https://docs.example.com"), false));

        var env = DomainTestHelper.ExpectValue(ClientEnvironment.Create(
            clientId,
            DomainTestHelper.EnvironmentName(),
            description: DomainTestHelper.Description(),
            username: DomainTestHelper.Username(),
            password: DomainTestHelper.Secret(),
            notes: DomainTestHelper.Body(),
            urls: new[] { primaryUrl, secondaryUrl }));

        Assert.Equal(2, env.PersistenceUrls.Count);
        Assert.Equal(2, env.Urls.Count);

        // Adding a new URL raises an event
        var addResult = env.AddUrl(DomainTestHelper.UrlType("api"), DomainTestHelper.Url("https://api.example.com"), true);
        Assert.True(addResult.IsSuccess);
        var added = Assert.IsType<EnvironmentUrlAdded>(env.DequeueEvents().Single());
        Assert.Equal("api", added.TypeCode.ToString());

        // Update the URL to become secondary
        var updateResult = env.UpdateUrl(added.UrlId, DomainTestHelper.UrlType("api"), DomainTestHelper.Url("https://api.example.com/v2"), false);
        Assert.True(updateResult.IsSuccess);
        var updated = Assert.IsType<EnvironmentUrlUpdated>(env.DequeueEvents().Single());
        Assert.False(updated.IsPrimary);

        // Remove the URL
        var removeResult = env.RemoveUrl(added.UrlId);
        Assert.True(removeResult.IsSuccess);
        Assert.IsType<EnvironmentUrlRemoved>(env.DequeueEvents().Single());

        // Ensure persistence collection round-trips
        env.PersistenceUrls.Add(new ClientEnvironment.ClientEnvironmentUrlRecord
        {
            Id = Guid.NewGuid(),
            TypeCode = "support",
            Url = "https://support.example.com",
            IsPrimary = false
        });
        Assert.Equal(3, env.Urls.Count);
    }

    [Fact]
    public void PrimaryPerType_IsStrictlyEnforced()
    {
        var type = DomainTestHelper.UrlType("portal");
        var primary = DomainTestHelper.ExpectValue(EnvUrl.Create(type, DomainTestHelper.Url("https://portal.example.com"), true));
        var conflicting = DomainTestHelper.ExpectValue(EnvUrl.Create(type, DomainTestHelper.Url("https://portal-two.example.com"), true));

        var failure = ClientEnvironment.Create(
            ClientEnvironmentId.New(),
            ClientId.New(),
            DomainTestHelper.EnvironmentName(),
            urls: new[] { primary, conflicting });

        Assert.True(failure.IsFailure);
        Assert.Equal("ENV_URL_PRIMARY_CONFLICT", failure.Error.Code);

        var env = DomainTestHelper.ExpectValue(ClientEnvironment.Create(
            ClientId.New(),
            DomainTestHelper.EnvironmentName()));

        // Add primary then attempt conflicting add
        Assert.True(env.AddUrl(type, DomainTestHelper.Url(), true).IsSuccess);
        var addConflict = env.AddUrl(type, DomainTestHelper.Url("https://alt.example.com"), true);
        Assert.True(addConflict.IsFailure);
        Assert.Equal("ENV_URL_PRIMARY_CONFLICT", addConflict.Error.Code);
    }

    [Fact]
    public void UpdateUrl_PrimaryConflictRejected()
    {
        var clientId = ClientId.New();
        var portalPrimary = DomainTestHelper.ExpectValue(EnvUrl.Create(DomainTestHelper.UrlType("portal"), DomainTestHelper.Url("https://portal.example.com"), true));
        var portalSecondary = DomainTestHelper.ExpectValue(EnvUrl.Create(DomainTestHelper.UrlType("portal"), DomainTestHelper.Url("https://portal-alt.example.com"), false));
        var env = DomainTestHelper.ExpectValue(ClientEnvironment.Create(clientId, DomainTestHelper.EnvironmentName(), urls: new[] { portalPrimary, portalSecondary }));

        var conflict = env.UpdateUrl(portalSecondary.Id, DomainTestHelper.UrlType("portal"), DomainTestHelper.Url("https://portal-alt.example.com"), true);
        Assert.True(conflict.IsFailure);
        Assert.Equal("ENV_URL_PRIMARY_CONFLICT", conflict.Error.Code);
    }
}
