using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using YinaCRM.Core.Builders;

namespace YinaCRM.Core.Tests;

public sealed class BuilderTests
{
    [Fact]
    public void ClientBuilder_ValidatesAndBuilds()
    {
        var builder = new ClientBuilder()
            .WithYinaYinaId(42)
            .WithInternalName("acme-client")
            .WithCompanyName("Acme")
            .AddTag("vip");

        var result = builder.Build();
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ClientBuilder_AggregatesErrors()
    {
        var builder = new ClientBuilder()
            .WithYinaYinaId(0)
            .WithInternalName("??");

        var result = builder.Build();
        Assert.True(result.IsFailure);
        Assert.Equal("CLIENT_BUILDER_INVALID", result.Error.Code);
    }

    [Fact]
    public void ClientBuilder_CollectsMultipleErrors()
    {
        var builder = new ClientBuilder()
            .WithYinaYinaId(0)
            .WithInternalName("??")
            .AddTag(" ");

        var result = builder.Build();
        Assert.True(result.IsFailure);
        Assert.Equal("CLIENT_BUILDER_INVALID", result.Error.Code);
        Assert.Contains("internal name", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tag", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClientEnvironmentBuilder_Validates()
    {
        var envBuilder = new ClientEnvironmentBuilder()
            .ForClient(ClientId.New())
            .WithName("Production")
            .AddUrl("portal", "https://portal.example.com", true);

        var result = envBuilder.Build();
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ClientEnvironmentBuilder_ReportsErrors()
    {
        var builder = new ClientEnvironmentBuilder()
            .AddUrl("portal", "invalid", true);

        var result = builder.Build();
        Assert.True(result.IsFailure);
    }
}

