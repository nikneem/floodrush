using HexMaster.FloodRush.Server.Profiles.Authentication;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Authentication;

public sealed class RsaKeyMaterialTests
{
    [Fact]
    public void Constructor_GeneratesUniqueKeyId()
    {
        using var key1 = new RsaKeyMaterial();
        using var key2 = new RsaKeyMaterial();

        Assert.NotEqual(key1.KeyId, key2.KeyId);
    }

    [Fact]
    public void Constructor_SetsKeyId_With16Characters()
    {
        using var key = new RsaKeyMaterial();

        Assert.Equal(16, key.KeyId.Length);
    }

    [Fact]
    public void Constructor_CreatesSecurityKey_WithMatchingKeyId()
    {
        using var key = new RsaKeyMaterial();

        Assert.Equal(key.KeyId, key.SecurityKey.KeyId);
    }

    [Fact]
    public void Constructor_SetsCreatedAtUtc_ToRecentTime()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        using var key = new RsaKeyMaterial();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.InRange(key.CreatedAtUtc, before, after);
    }

    [Fact]
    public void Constructor_CreatesRsaSecurityKey_With2048BitKey()
    {
        using var key = new RsaKeyMaterial();

        Assert.Equal(2048, key.SecurityKey.Rsa!.KeySize);
    }

    [Fact]
    public void IsExpiredFor_ReturnsFalse_ForJustCreatedKey()
    {
        using var key = new RsaKeyMaterial();

        var expired = key.IsExpiredFor(tokenLifetimeMinutes: 60, rotationIntervalMinutes: 120);

        Assert.False(expired);
    }

    [Fact]
    public void IsExpiredFor_ReturnsTrue_WhenRetentionPeriodHasElapsed()
    {
        // Simulate a key that was created long ago by using a very short retention window
        using var key = new RsaKeyMaterial();

        // With 0 minutes lifetime + 0 rotation, the key is already expired
        // (retention = 0, but UtcNow - CreatedAt is a tiny positive value)
        // We use negative values to force expiry without waiting
        var expired = key.IsExpiredFor(tokenLifetimeMinutes: -1, rotationIntervalMinutes: -1);

        Assert.True(expired);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var key = new RsaKeyMaterial();
        var ex = Record.Exception(key.Dispose);

        Assert.Null(ex);
    }
}
