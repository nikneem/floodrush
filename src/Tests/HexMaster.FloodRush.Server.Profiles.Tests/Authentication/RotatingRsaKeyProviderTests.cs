using HexMaster.FloodRush.Server.Profiles.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Authentication;

public sealed class RotatingRsaKeyProviderTests : IAsyncLifetime
{
    private readonly RotatingRsaKeyProvider _provider;

    public RotatingRsaKeyProviderTests()
    {
        var options = Options.Create(new DeviceTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            TokenLifetimeMinutes = 60,
            KeyRotationIntervalMinutes = 1440
        });

        _provider = new RotatingRsaKeyProvider(options, NullLogger<RotatingRsaKeyProvider>.Instance);
    }

    public async Task InitializeAsync() =>
        await _provider.StartAsync(CancellationToken.None);

    public async Task DisposeAsync()
    {
        await _provider.StopAsync(CancellationToken.None);
        _provider.Dispose();
    }

    [Fact]
    public void GetCurrentSigningCredentials_ReturnsRsaSha256Credentials()
    {
        var credentials = _provider.GetCurrentSigningCredentials();

        Assert.Equal(SecurityAlgorithms.RsaSha256, credentials.Algorithm);
        Assert.IsType<RsaSecurityKey>(credentials.Key);
    }

    [Fact]
    public void GetCurrentSigningCredentials_KeyHasNonEmptyKeyId()
    {
        var credentials = _provider.GetCurrentSigningCredentials();

        Assert.NotNull(credentials.Key.KeyId);
        Assert.NotEmpty(credentials.Key.KeyId);
    }

    [Fact]
    public void GetAllValidationKeys_ReturnsAtLeastOneKey()
    {
        var keys = _provider.GetAllValidationKeys().ToList();

        Assert.NotEmpty(keys);
    }

    [Fact]
    public void GetAllValidationKeys_ContainsCurrentSigningKey()
    {
        var currentKid = _provider.GetCurrentSigningCredentials().Key.KeyId;
        var validationKeys = _provider.GetAllValidationKeys().ToList();

        Assert.Contains(validationKeys, k => k.KeyId == currentKid);
    }

    [Fact]
    public void GetPublicKeySet_ContainsPublicKeyForCurrentSigningKey()
    {
        var currentKid = _provider.GetCurrentSigningCredentials().Key.KeyId;
        var keySet = _provider.GetPublicKeySet();

        Assert.Contains(keySet.Keys, k => k.KeyId == currentKid);
    }

    [Fact]
    public void GetPublicKeySet_KeysHaveSignatureUse()
    {
        var keySet = _provider.GetPublicKeySet();

        Assert.All(keySet.Keys, k => Assert.Equal("sig", k.Use));
    }

    [Fact]
    public void GetPublicKeySet_KeysAreRsaType()
    {
        var keySet = _provider.GetPublicKeySet();

        Assert.All(keySet.Keys, k => Assert.Equal("RSA", k.Kty));
    }

    [Fact]
    public void GetPublicKeySet_KeysDoNotExposePrivateKeyMaterial()
    {
        var keySet = _provider.GetPublicKeySet();

        Assert.All(keySet.Keys, k =>
        {
            Assert.True(string.IsNullOrEmpty(k.D), "Private exponent d should not be exported");
        });
    }
}
