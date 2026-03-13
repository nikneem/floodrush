using HexMaster.FloodRush.Server.Profiles.Authentication;
using HexMaster.FloodRush.Server.Profiles.Data;
using HexMaster.FloodRush.Server.Profiles.Features.DeviceLogin;
using HexMaster.FloodRush.Server.Profiles.Features.GetCurrentProfile;
using HexMaster.FloodRush.Server.Profiles.Features.UpdateProfile;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Features;

public sealed class LoginDeviceCommandHandlerTests
{
    private readonly StubPlayerProfilesRepository _repo = new();
    private readonly DeviceTokenService _tokenService;

    public LoginDeviceCommandHandlerTests()
    {
        var tokenOptions = Options.Create(new DeviceTokenOptions
        {
            Issuer = "test",
            Audience = "test",
            TokenLifetimeMinutes = 60,
            KeyRotationIntervalMinutes = 120
        });
        _tokenService = new DeviceTokenService(tokenOptions, new StubTokenSigningKeyProvider());
    }

    [Fact]
    public async Task HandleAsync_WithValidDeviceId_ReturnsToken()
    {
        _repo.Profile = new PlayerProfileDto("profile-1", "device-12345678", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        var result = await handler.HandleAsync(new LoginDeviceCommand("device-12345678"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task HandleAsync_TrimsDeviceId_BeforeValidation()
    {
        _repo.Profile = new PlayerProfileDto("profile-1", "device-12345678", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        var result = await handler.HandleAsync(new LoginDeviceCommand("  device-12345678  "), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("device-12345678", _repo.LastDeviceId);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task HandleAsync_ThrowsArgumentException_WhenDeviceIdTooShort(string deviceId)
    {
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new LoginDeviceCommand(deviceId), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentException_WhenDeviceIdTooLong()
    {
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new LoginDeviceCommand(new string('a', 201)), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task HandleAsync_AcceptsDeviceId_AtMinimumLength()
    {
        var minLength = new string('a', 8);
        _repo.Profile = new PlayerProfileDto("profile-1", minLength, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        var result = await handler.HandleAsync(new LoginDeviceCommand(minLength), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("device@invalid")]
    [InlineData("device#bad")]
    [InlineData("device with spaces")]
    public async Task HandleAsync_ThrowsArgumentException_WhenDeviceIdHasInvalidCharacters(string deviceId)
    {
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new LoginDeviceCommand(deviceId), CancellationToken.None).AsTask());
    }

    [Theory]
    [InlineData("device-12345678")]
    [InlineData("Device.Name:123")]
    [InlineData("device_id-123.abc")]
    public async Task HandleAsync_AcceptsDeviceId_WithAllowedCharacters(string deviceId)
    {
        _repo.Profile = new PlayerProfileDto("profile-1", deviceId, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var handler = new LoginDeviceCommandHandler(_repo, _tokenService);

        var result = await handler.HandleAsync(new LoginDeviceCommand(deviceId), CancellationToken.None);

        Assert.NotNull(result);
    }
}

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly StubPlayerProfilesRepository _repo = new();

    [Fact]
    public async Task HandleAsync_UpdatesDisplayName_WithValidInput()
    {
        var expected = new PlayerProfileDto("profile-1", "device-abc12345", "New Name", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _repo.Profile = expected;
        var handler = new UpdateProfileCommandHandler(_repo);

        var result = await handler.HandleAsync(
            new UpdateProfileCommand("device-abc12345", "New Name"),
            CancellationToken.None);

        Assert.Equal(expected, result);
        Assert.Equal("New Name", _repo.LastDisplayName);
    }

    [Fact]
    public async Task HandleAsync_TrimsDisplayName_BeforeValidation()
    {
        _repo.Profile = new PlayerProfileDto("p", "d", "Trimmed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var handler = new UpdateProfileCommandHandler(_repo);

        await handler.HandleAsync(
            new UpdateProfileCommand("device-abc12345", "  Trimmed  "),
            CancellationToken.None);

        Assert.Equal("Trimmed", _repo.LastDisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ThrowsArgumentException_WhenDisplayNameEmpty(string displayName)
    {
        var handler = new UpdateProfileCommandHandler(_repo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new UpdateProfileCommand("device-abc12345", displayName),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentException_WhenDisplayNameTooLong()
    {
        var handler = new UpdateProfileCommandHandler(_repo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new UpdateProfileCommand("device-abc12345", new string('x', 51)),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task HandleAsync_AcceptsDisplayName_AtMaximumLength()
    {
        var maxLength = new string('x', 50);
        _repo.Profile = new PlayerProfileDto("p", "d", maxLength, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var handler = new UpdateProfileCommandHandler(_repo);

        var result = await handler.HandleAsync(
            new UpdateProfileCommand("device-abc12345", maxLength),
            CancellationToken.None);

        Assert.NotNull(result);
    }
}

public sealed class GetCurrentProfileQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsProfile_WhenFound()
    {
        var expected = new PlayerProfileDto("profile-1", "device-12345678", "Alice", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var repo = new StubPlayerProfilesRepository { Profile = expected };
        var handler = new GetCurrentProfileQueryHandler(repo);

        var result = await handler.HandleAsync(
            new GetCurrentProfileQuery("device-12345678"),
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_ThrowsInvalidOperationException_WhenProfileNotFound()
    {
        var repo = new StubPlayerProfilesRepository { Profile = null };
        var handler = new GetCurrentProfileQueryHandler(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new GetCurrentProfileQuery("device-missing"),
                CancellationToken.None).AsTask());
    }
}

/// <summary>Simple stub for ITokenSigningKeyProvider that uses a fresh RSA key without needing a started background service.</summary>
internal sealed class StubTokenSigningKeyProvider : ITokenSigningKeyProvider
{
    private readonly RsaKeyMaterial _key = new();

    public SigningCredentials GetCurrentSigningCredentials() =>
        new(_key.SecurityKey, SecurityAlgorithms.RsaSha256);

    public IEnumerable<SecurityKey> GetAllValidationKeys() => [_key.SecurityKey];

    public JsonWebKeySet GetPublicKeySet() => new();
}

/// <summary>Manual stub for IPlayerProfilesRepository.</summary>
internal sealed class StubPlayerProfilesRepository : IPlayerProfilesRepository
{
    public PlayerProfileDto? Profile { get; set; }
    public string? LastDeviceId { get; private set; }
    public string? LastDisplayName { get; private set; }

    public ValueTask<PlayerProfileDto> GetOrCreateByDeviceIdAsync(
        string deviceId, CancellationToken cancellationToken)
    {
        LastDeviceId = deviceId;
        return ValueTask.FromResult(Profile ?? throw new InvalidOperationException("No profile configured in stub."));
    }

    public ValueTask<PlayerProfileDto?> GetByDeviceIdAsync(
        string deviceId, CancellationToken cancellationToken)
    {
        LastDeviceId = deviceId;
        return ValueTask.FromResult(Profile);
    }

    public ValueTask<PlayerProfileDto> UpdateDisplayNameAsync(
        string deviceId, string displayName, CancellationToken cancellationToken)
    {
        LastDeviceId = deviceId;
        LastDisplayName = displayName;
        return ValueTask.FromResult(Profile ?? throw new InvalidOperationException("No profile configured in stub."));
    }
}
