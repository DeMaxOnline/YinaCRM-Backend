using System;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using ClientUserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.ClientUser.ClientUserIdTag>;
using YinaCRM.Core.Entities.ClientUser;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;

namespace YinaCRM.Core.Tests;

public sealed class ClientUserTests
{
    [Fact]
    public void CreateAndUpdate_ClientUser()
    {
        var clientId = ClientId.New();
        var display = DomainTestHelper.ExpectValue(YinaCRM.Core.Entities.ClientUser.VOs.DisplayName.TryCreate("Jane Doe"));
        var email = DomainTestHelper.Email("jane@example.com");
        var phone = DomainTestHelper.Phone("+1 999 888 7777");
        var role = DomainTestHelper.RoleName("manager");

        var clientUser = DomainTestHelper.ExpectValue(ClientUser.Create(clientId, display, email, phone, role));
        Assert.Equal(email, clientUser.Email);
        Assert.Equal(display, clientUser.DisplayName);
        Assert.Null(clientUser.UpdatedAt);

        var newDisplay = DomainTestHelper.ExpectValue(YinaCRM.Core.Entities.ClientUser.VOs.DisplayName.TryCreate("Janet Doe"));
        var result = clientUser.Update(newDisplay, email, phone, role);
        Assert.True(result.IsSuccess);
        Assert.Equal(newDisplay, clientUser.DisplayName);
        Assert.NotNull(clientUser.UpdatedAt);

        // Updating with same values is a no-op
        var noChange = clientUser.Update(newDisplay, email, phone, role);
        Assert.True(noChange.IsSuccess);
    }

    [Fact]
    public void Create_WithEmptyDisplayName_Fails()
    {
        var fail = ClientUser.Create(ClientUserId.New(), ClientId.New(), default, null, null, null);
        Assert.True(fail.IsFailure);
        Assert.Equal("CLIENTUSER_DISPLAYNAME_REQUIRED", fail.Error.Code);
    }
}


