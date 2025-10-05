using System;
using InteractionId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.InteractionIdTag>;
using YinaCRM.Core.Entities.Hardware.VOs;
using YinaCRM.Core.Entities.Interaction;
using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Entities.Note.VOs;
using YinaCRM.Core.Entities.SupportTicket.VOs;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;
using YinaCRM.Core.ValueObjects.AddressVO.CityVO;
using YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
using YinaCRM.Core.ValueObjects.Identity.PhoneVO;

namespace YinaCRM.Core.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void EmailValidation()
    {
        Assert.True(Email.TryCreate("bad").IsFailure);
        Assert.Equal("EMAIL_EMPTY", Email.TryCreate(string.Empty).Error.Code);
        Assert.StartsWith("valid", DomainTestHelper.Email("valid@example.com").ToString());
    }

    [Fact]
    public void PhoneValidation()
    {
        Assert.True(Phone.TryCreate("abc").IsFailure);
    }

    [Fact]
    public void AddressValidation()
    {
        Assert.True(AddressLine.TryCreate(null).IsFailure);
        Assert.True(City.TryCreate(string.Empty).IsFailure);
        Assert.True(PostalCode.TryCreate("???").IsFailure);
    }

    [Fact]
    public void InternalNameValidation()
    {
        Assert.True(YinaCRM.Core.Entities.Client.VOs.InternalName.TryCreate("??").IsFailure);
    }

    [Fact]
    public void EnvironmentNameValidation()
    {
        Assert.True(YinaCRM.Core.Entities.ClientEnvironment.VOs.EnvironmentName.TryCreate(" ").IsFailure);
    }

    [Fact]
    public void UrlValidation()
    {
        Assert.True(Url.TryCreate("not-a-url").IsFailure);
        Assert.True(UrlTypeCode.TryCreate("with space").IsFailure);
    }

    [Fact]
    public void MoneyValidation()
    {
        Assert.True(CurrencyCode.TryCreate("US").IsFailure);
        var usd = DomainTestHelper.ExpectValue(CurrencyCode.TryCreate("USD"));
        Assert.True(Money.TryCreate(-1, usd).IsFailure);
    }

    [Fact]
    public void HardwareValueValidation()
    {
        Assert.True(HardwareTypeCode.TryCreate(null).IsFailure);
        Assert.True(AnyDeskId.TryCreate("abc").IsFailure);
        Assert.True(ExternalHardwareId.TryCreate("*").IsFailure);
    }

    [Fact]
    public void InteractionLinkValidation()
    {
        var id = InteractionId.New();
        Assert.True(InteractionLink.Create(id, string.Empty, Guid.NewGuid()).IsFailure);
        Assert.True(InteractionLink.Create(id, "ticket", Guid.Empty).IsFailure);
        Assert.True(InteractionParticipant.Create(id, DomainTestHelper.ActorKind("User"), Guid.Empty, DomainTestHelper.ParticipantRole("organizer")).IsFailure);
        Assert.True(InteractionParticipant.Create(id, DomainTestHelper.ActorKind("User"), Guid.Empty, DomainTestHelper.ParticipantRole("organizer")).IsFailure);
    }

    [Fact]
    public void TicketValueValidation()
    {
        Assert.True(TicketNumber.TryCreate("BAD").IsFailure);
        Assert.True(TicketStatusCode.TryCreate("invalid").IsFailure);
        Assert.True(TicketPriorityCode.TryCreate("invalid").IsFailure);
        Assert.False(TicketStatusCode.Closed.CanTransitionTo(TicketStatusCode.Waiting));
    }

    [Fact]
    public void VisibilityValidation()
    {
        Assert.True(VisibilityCode.TryCreate("public").IsFailure);
    }
}








