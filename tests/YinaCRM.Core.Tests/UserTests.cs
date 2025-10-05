using System;
using UserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.User.UserIdTag>;
using YinaCRM.Core.Entities.User;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;

namespace YinaCRM.Core.Tests;

public sealed class UserTests
{
    [Fact]
    public void CreateAndUpdate_User()
    {
        var authSub = DomainTestHelper.AuthSubject("auth|456");
        var displayName = DomainTestHelper.ExpectValue(YinaCRM.Core.Entities.User.VOs.DisplayName.TryCreate("Agent Smith"));
        var email = DomainTestHelper.Email("agent@corp.example");
        var user = DomainTestHelper.ExpectValue(User.Create(authSub, displayName, email, DomainTestHelper.TimeZone(), DomainTestHelper.Locale()));

        Assert.Equal(authSub, user.AuthSub);
        Assert.Equal(email, user.Email);

        var newDisplay = DomainTestHelper.ExpectValue(YinaCRM.Core.Entities.User.VOs.DisplayName.TryCreate("Agent Neo"));
        var newEmail = DomainTestHelper.Email("neo@corp.example");
        Assert.True(user.UpdateProfile(newDisplay, newEmail, DomainTestHelper.TimeZone("Europe/Paris"), DomainTestHelper.Locale("fr-FR")).IsSuccess);
        Assert.Equal(newDisplay, user.DisplayName);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyAuthSubject_Fails()
    {
        var displayName = DomainTestHelper.ExpectValue(YinaCRM.Core.Entities.User.VOs.DisplayName.TryCreate("Agent"));
        var email = DomainTestHelper.Email("agent@example.com");
        var fail = User.Create(UserId.New(), default, displayName, email);
        Assert.True(fail.IsFailure);
        Assert.Equal("USER_AUTHSUB_REQUIRED", fail.Error.Code);
    }
}
