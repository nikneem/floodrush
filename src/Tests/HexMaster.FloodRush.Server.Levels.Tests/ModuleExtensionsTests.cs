using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HexMaster.FloodRush.Server.Levels.Tests;

public sealed class ModuleExtensionsTests
{
    [Fact]
    public void AddLevelsModule_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddLevelsModule(config);

        Assert.True(services.Count > 0);
    }

    [Fact]
    public void AddLevelsModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var result = services.AddLevelsModule(config);

        Assert.Same(services, result);
    }
}
