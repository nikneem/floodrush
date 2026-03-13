using HexMaster.FloodRush.Server.Profiles.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Authentication;

public sealed class ConfigureDeviceJwtBearerOptionsTests
{
    private readonly ConfigureDeviceJwtBearerOptions _sut;

    public ConfigureDeviceJwtBearerOptionsTests()
    {
        var tokenOptions = Options.Create(new DeviceTokenOptions
        {
            Issuer = "https://test.example.com",
            Audience = "floodrush-client",
            TokenLifetimeMinutes = 60,
            KeyRotationIntervalMinutes = 120
        });
        _sut = new ConfigureDeviceJwtBearerOptions(tokenOptions, new StubSigningKeyProvider());
    }

    [Fact]
    public void Configure_DefaultScheme_SetsTokenValidationParameters()
    {
        var options = new JwtBearerOptions();

        _sut.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        Assert.NotNull(options.TokenValidationParameters);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.Equal("https://test.example.com", options.TokenValidationParameters.ValidIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.Equal("floodrush-client", options.TokenValidationParameters.ValidAudience);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.Equal(TimeSpan.FromSeconds(30), options.TokenValidationParameters.ClockSkew);
    }

    [Fact]
    public void Configure_NullName_SetsTokenValidationParameters()
    {
        var options = new JwtBearerOptions();

        _sut.Configure(null, options);

        Assert.NotNull(options.TokenValidationParameters);
    }

    [Fact]
    public void Configure_DifferentScheme_DoesNotModifyOptions()
    {
        var options = new JwtBearerOptions();
        var originalIssuers = options.TokenValidationParameters.ValidIssuer;

        _sut.Configure("OtherScheme", options);

        // The ValidIssuer should not have been set (early return)
        Assert.Null(options.TokenValidationParameters.ValidIssuer);
        Assert.Equal(originalIssuers, options.TokenValidationParameters.ValidIssuer);
    }

    [Fact]
    public void Configure_NoScheme_DelegatesToNamedConfigure()
    {
        var options = new JwtBearerOptions();

        _sut.Configure(options);

        Assert.NotNull(options.TokenValidationParameters);
        Assert.Equal("https://test.example.com", options.TokenValidationParameters.ValidIssuer);
    }

    [Fact]
    public void Configure_IssuerSigningKeyResolver_ReturnsKeysFromProvider()
    {
        var options = new JwtBearerOptions();
        _sut.Configure(options);

        var keys = options.TokenValidationParameters.IssuerSigningKeyResolver(
            "token", null!, null, options.TokenValidationParameters).ToList();

        Assert.NotEmpty(keys);
    }
}

internal sealed class StubSigningKeyProvider : ITokenSigningKeyProvider
{
    private readonly RsaKeyMaterial _key = new();

    public SigningCredentials GetCurrentSigningCredentials() =>
        new(_key.SecurityKey, SecurityAlgorithms.RsaSha256);

    public IEnumerable<SecurityKey> GetAllValidationKeys() => [_key.SecurityKey];

    public JsonWebKeySet GetPublicKeySet() => new();
}
