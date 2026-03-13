using HexMaster.FloodRush.Server.Abstractions.Storage;
using HexMaster.FloodRush.Server.Levels.Data;
using Microsoft.Extensions.Configuration;

namespace HexMaster.FloodRush.Server.Levels.Tests.Data;

public sealed class TableLevelsRepositoryTests
{
    private static TableLevelsRepository BuildRepository(string? connectionString = "UseDevelopmentStorage=true")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(connectionString is null
                ? []
                : [new KeyValuePair<string, string?>(
                    $"ConnectionStrings:{StorageResourceNames.Tables}",
                    connectionString)])
            .Build();
        return new TableLevelsRepository(config, new BuiltInLevelsCatalog(), new BasicLevelsSeedCatalog());
    }

    [Fact]
    public void Constructor_ValidConnectionString_DoesNotThrow()
    {
        var ex = Record.Exception(() => BuildRepository());

        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_MissingConnectionString_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => BuildRepository(null));
    }

    [Fact]
    public async Task GetLevelRevisionAsync_NullLevelId_ThrowsArgumentException()
    {
        var repo = BuildRepository();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repo.GetLevelRevisionAsync("profile", null!, "v1", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetLevelRevisionAsync_NullRevision_ThrowsArgumentException()
    {
        var repo = BuildRepository();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repo.GetLevelRevisionAsync("profile", "level-001", null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetLevelRevisionAsync_WhitespaceLevelId_ThrowsArgumentException()
    {
        var repo = BuildRepository();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repo.GetLevelRevisionAsync("profile", "   ", "v1", CancellationToken.None).AsTask());
    }
}
