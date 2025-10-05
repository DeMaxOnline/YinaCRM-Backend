using System;
using System.Linq;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.ClientEnvironment;
using YinaCRM.Core.Entities.ClientEnvironment.VOs;
using YinaCRM.Core.Entities.Hardware.VOs;
using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Entities.Note.VOs;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;
using YinaCRM.Core.ValueObjects.AddressVO.CityVO;
using YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name;
using YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;
using YinaCRM.Core.ValueObjects.Identity.AuthSubjectVO;
using YinaCRM.Core.ValueObjects.Identity.EmailVO;
using YinaCRM.Core.ValueObjects.Identity.PhoneVO;
using YinaCRM.Core.ValueObjects.Identity.SecretVO;
using YinaCRM.Core.ValueObjects.Identity.UsernameVO;
using InternalNameVo = YinaCRM.Core.Entities.Client.VOs.InternalName;
using EnvironmentNameVo = YinaCRM.Core.Entities.ClientEnvironment.VOs.EnvironmentName;
using CompanyNameVo = YinaCRM.Core.ValueObjects.CompanyName;
using CommercialNameVo = YinaCRM.Core.ValueObjects.CommercialName;
using EmailVo = YinaCRM.Core.ValueObjects.Identity.EmailVO.Email;
using PhoneVo = YinaCRM.Core.ValueObjects.Identity.PhoneVO.Phone;
using AddressLineVo = YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO.AddressLine;
using CityVo = YinaCRM.Core.ValueObjects.AddressVO.CityVO.City;
using PostalCodeVo = YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO.PostalCode;
using CountryNameVo = YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name.CountryName;
using TagVo = YinaCRM.Core.ValueObjects.Tag;
using UsernameVo = YinaCRM.Core.ValueObjects.Username;
using SecretVo = YinaCRM.Core.ValueObjects.Identity.SecretVO.Secret;
using BodyVo = YinaCRM.Core.ValueObjects.Body;
using TitleVo = YinaCRM.Core.ValueObjects.Title;
using DescriptionVo = YinaCRM.Core.ValueObjects.Description;
using ModuleNameVo = YinaCRM.Core.ValueObjects.ModuleName;
using PlanNameVo = YinaCRM.Core.ValueObjects.PlanName;
using RoleNameVo = YinaCRM.Core.ValueObjects.RoleName;
using UrlVo = YinaCRM.Core.ValueObjects.Url;
using UrlTypeCodeVo = YinaCRM.Core.ValueObjects.UrlTypeCode;
using ActorKindCodeVo = YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO.ActorKindCode;
using MoneyVo = YinaCRM.Core.ValueObjects.Money;
using CurrencyCodeVo = YinaCRM.Core.ValueObjects.CurrencyCode;
using LocaleCodeVo = YinaCRM.Core.ValueObjects.LocaleCode;
using TimeZoneIdVo = YinaCRM.Core.ValueObjects.TimeZoneId;
using AuthSubjectVo = YinaCRM.Core.ValueObjects.Identity.AuthSubjectVO.AuthSubject;
using ExternalHardwareIdVo = YinaCRM.Core.Entities.Hardware.VOs.ExternalHardwareId;
using HardwareTypeCodeVo = YinaCRM.Core.Entities.Hardware.VOs.HardwareTypeCode;
using HardwareDetailTypeCodeVo = YinaCRM.Core.Entities.Hardware.VOs.HardwareDetailTypeCode;
using BrandVo = YinaCRM.Core.Entities.Hardware.VOs.Brand;
using ModelVo = YinaCRM.Core.Entities.Hardware.VOs.Model;
using SerialNumberVo = YinaCRM.Core.Entities.Hardware.VOs.SerialNumber;
using IpAddressVo = YinaCRM.Core.Entities.Hardware.VOs.IpAddress;
using AnyDeskIdVo = YinaCRM.Core.Entities.Hardware.VOs.AnyDeskId;
using InteractionTypeCodeVo = YinaCRM.Core.Entities.Interaction.VOs.InteractionTypeCode;
using InteractionDirectionCodeVo = YinaCRM.Core.Entities.Interaction.VOs.InteractionDirectionCode;
using ParticipantRoleCodeVo = YinaCRM.Core.Entities.Interaction.VOs.ParticipantRoleCode;
using VisibilityCodeVo = YinaCRM.Core.Entities.Note.VOs.VisibilityCode;

namespace YinaCRM.Core.Tests;

internal static class DomainTestHelper
{
    public static T ExpectValue<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error.ToString());
        return result.Value;
    }

    public static InternalNameVo InternalName(string value = "acme-co")
        => ExpectValue(InternalNameVo.TryCreate(value));

    public static CompanyNameVo CompanyName(string value = "Acme Corporation")
        => ExpectValue(CompanyNameVo.TryCreate(value));

    public static CommercialNameVo CommercialName(string value = "Acme")
        => ExpectValue(CommercialNameVo.TryCreate(value));

    public static EmailVo Email(string value = "primary@example.com")
        => ExpectValue(EmailVo.TryCreate(value));

    public static PhoneVo Phone(string value = "+1 (222) 333-4444")
        => ExpectValue(PhoneVo.TryCreate(value));

    public static AddressLineVo AddressLine(string value = "123 Main St")
        => ExpectValue(AddressLineVo.TryCreate(value));

    public static CityVo City(string value = "Metropolis")
        => ExpectValue(CityVo.TryCreate(value));

    public static PostalCodeVo PostalCode(string value = "12345")
        => ExpectValue(PostalCodeVo.TryCreate(value));

    public static CountryNameVo Country(string value = "Wonderland")
        => ExpectValue(CountryNameVo.TryCreate(value));

    public static TagVo Tag(string value = "vip")
        => ExpectValue(TagVo.TryCreate(value));

    public static TagVo[] Tags(params string[] values)
        => values.Select(Tag).ToArray();

    public static EnvironmentNameVo EnvironmentName(string value = "Production")
        => ExpectValue(EnvironmentNameVo.TryCreate(value));

    public static UsernameVo Username(string value = "operator")
        => ExpectValue(UsernameVo.TryCreate(value));

    public static SecretVo Secret(string value = "Sup3rSecret!")
        => ExpectValue(SecretVo.TryCreate(value));

    public static BodyVo Body(string value = "Sample body")
        => ExpectValue(BodyVo.TryCreate(value));

    public static TitleVo Title(string value = "Follow up call")
        => ExpectValue(TitleVo.TryCreate(value));

    public static DescriptionVo Description(string value = "Detailed description")
        => ExpectValue(DescriptionVo.TryCreate(value));

    public static ModuleNameVo ModuleName(string value = "crm")
        => ExpectValue(ModuleNameVo.TryCreate(value));

    public static PlanNameVo PlanName(string value = "pro")
        => ExpectValue(PlanNameVo.TryCreate(value));

    public static RoleNameVo RoleName(string value = "admin")
        => ExpectValue(RoleNameVo.TryCreate(value));

    public static UrlVo Url(string value = "https://app.example.com")
        => ExpectValue(UrlVo.TryCreate(value));

    public static UrlTypeCodeVo UrlType(string value = "portal")
        => ExpectValue(UrlTypeCodeVo.TryCreate(value));

    public static ActorKindCodeVo ActorKind(string value = "User")
        => ExpectValue(ActorKindCodeVo.TryCreate(value));

    public static MoneyVo Money(decimal amount = 10m, string currency = "USD")
    {
        var code = ExpectValue(CurrencyCodeVo.TryCreate(currency));
        return ExpectValue(MoneyVo.TryCreate(amount, code));
    }

    public static LocaleCodeVo Locale(string value = "en-US")
        => ExpectValue(LocaleCodeVo.TryCreate(value));

    public static TimeZoneIdVo TimeZone(string value = "UTC")
        => ExpectValue(TimeZoneIdVo.TryCreate(value));

    public static AuthSubjectVo AuthSubject(string value = "auth|123")
        => ExpectValue(AuthSubjectVo.TryCreate(value));

    public static ExternalHardwareIdVo ExternalHardwareId(string value = "HW-123")
        => ExpectValue(ExternalHardwareIdVo.TryCreate(value));

    public static HardwareTypeCodeVo HardwareType(string value = "laptop")
        => ExpectValue(HardwareTypeCodeVo.TryCreate(value));

    public static HardwareDetailTypeCodeVo HardwareDetailType(string value = "ultrabook")
        => ExpectValue(HardwareDetailTypeCodeVo.TryCreate(value));

    public static SerialNumberVo SerialNumber(string value = "SN-999")
        => ExpectValue(SerialNumberVo.TryCreate(value));

    public static BrandVo Brand(string value = "Acme")
        => ExpectValue(BrandVo.TryCreate(value));

    public static ModelVo Model(string value = "X1000")
        => ExpectValue(ModelVo.TryCreate(value));

    public static IpAddressVo IpAddress(string value = "192.168.1.10")
        => ExpectValue(IpAddressVo.TryCreate(value));

    public static AnyDeskIdVo AnyDesk(string value = "123456789")
        => ExpectValue(AnyDeskIdVo.TryCreate(value));

    public static InteractionTypeCodeVo InteractionType(string value = "call")
        => ExpectValue(InteractionTypeCodeVo.TryCreate(value));

    public static InteractionDirectionCodeVo InteractionDirection(string value = "inbound")
        => ExpectValue(InteractionDirectionCodeVo.TryCreate(value));

    public static ParticipantRoleCodeVo ParticipantRole(string value = "organizer")
        => ExpectValue(ParticipantRoleCodeVo.TryCreate(value));

    public static VisibilityCodeVo Visibility(string value = "internal")
        => ExpectValue(VisibilityCodeVo.TryCreate(value));
}


