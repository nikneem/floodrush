using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HexMaster.FloodRush.Server.Profiles.Tests;

public sealed class ModuleExtensionsTests
{
    [Fact]
    public void AddProfilesModule_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeviceJwt:Issuer"] = "https://test.example.com",
                ["Authentication:DeviceJwt:Audience"] = "test-client",
                ["Authentication:DeviceJwt:TokenLifetimeMinutes"] = "60",
                ["Authentication:DeviceJwt:KeyRotationIntervalMinutes"] = "120"
            })
            .Build();

        services.AddProfilesModule(config);

        Assert.True(services.Count > 0);
    }

    [Fact]
    public void AddProfilesModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeviceJwt:Issuer"] = "https://test.example.com",
                ["Authentication:DeviceJwt:Audience"] = "test-client",
                ["Authentication:DeviceJwt:TokenLifetimeMinutes"] = "60",
                ["Authentication:DeviceJwt:KeyRotationIntervalMinutes"] = "120"
            })
            .Build();

        var result = services.AddProfilesModule(config);

        Assert.Same(services, result);
    }
}
