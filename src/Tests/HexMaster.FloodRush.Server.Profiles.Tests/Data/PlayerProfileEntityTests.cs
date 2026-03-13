using HexMaster.FloodRush.Server.Profiles.Data;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Data;

public sealed class PlayerProfileEntityTests
{
    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var registered = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var lastSeen = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var entity = new PlayerProfileEntity
        {
            ProfileId = "profile-abc",
            RowKey = "device-12345678",
            DisplayName = "Test Player",
            RegisteredAtUtc = registered,
            LastSeenAtUtc = lastSeen
        };

        var dto = entity.ToDto();

        Assert.Equal("profile-abc", dto.ProfileId);
        Assert.Equal("device-12345678", dto.DeviceId);
        Assert.Equal("Test Player", dto.DisplayName);
        Assert.Equal(registered, dto.RegisteredAtUtc);
        Assert.Equal(lastSeen, dto.LastSeenAtUtc);
    }

    [Fact]
    public void ToDto_NullDisplayName_PreservesNull()
    {
        var entity = new PlayerProfileEntity
        {
            ProfileId = "profile-abc",
            RowKey = "device-12345678",
            DisplayName = null,
            RegisteredAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();

        Assert.Null(dto.DisplayName);
    }

    [Fact]
    public void PartitionValue_IsProfile()
    {
        Assert.Equal("profile", PlayerProfileEntity.PartitionValue);
    }
}
