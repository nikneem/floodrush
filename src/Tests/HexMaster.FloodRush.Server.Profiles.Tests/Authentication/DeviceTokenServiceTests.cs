using System.IdentityModel.Tokens.Jwt;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Profiles.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Authentication;

public sealed class DeviceTokenServiceTests : IAsyncLifetime
{
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private readonly RotatingRsaKeyProvider _keyProvider;
    private readonly DeviceTokenService _service;

    public DeviceTokenServiceTests()
    {
        var options = Options.Create(new DeviceTokenOptions
        {
            Issuer = TestIssuer,
            Audience = TestAudience,
            TokenLifetimeMinutes = 60,
            KeyRotationIntervalMinutes = 1440
        });

        _keyProvider = new RotatingRsaKeyProvider(options, NullLogger<RotatingRsaKeyProvider>.Instance);
        _service = new DeviceTokenService(options, _keyProvider);
    }

    public async Task InitializeAsync() =>
        await _keyProvider.StartAsync(CancellationToken.None);

    public async Task DisposeAsync()
    {
        await _keyProvider.StopAsync(CancellationToken.None);
        _keyProvider.Dispose();
    }

    [Fact]
    public void CreateToken_ReturnsNonEmptyToken()
    {
        var response = _service.CreateToken("device-123", "profile-abc");

        Assert.NotNull(response.Token);
        Assert.NotEmpty(response.Token);
    }

    [Fact]
    public void CreateToken_ReturnsCorrectTokenType()
    {
        var response = _service.CreateToken("device-123", "profile-abc");

        Assert.Equal("Bearer", response.TokenType);
    }

    [Fact]
    public void CreateToken_ReturnsMatchingDeviceAndProfileIds()
    {
        var response = _service.CreateToken("device-123", "profile-abc");

        Assert.Equal("device-123", response.DeviceId);
        Assert.Equal("profile-abc", response.ProfileId);
    }

    [Fact]
    public void CreateToken_ExpiresInFuture()
    {
        var before = DateTimeOffset.UtcNow;
        var response = _service.CreateToken("device-123", "profile-abc");

        Assert.True(response.ExpiresAtUtc > before);
    }

    [Fact]
    public void CreateToken_ProducesValidJwtWithRsaSignature()
    {
        var response = _service.CreateToken("device-123", "profile-abc");

        var handler = new JwtSecurityTokenHandler();
        var validationKeys = _keyProvider.GetAllValidationKeys().ToList();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = validationKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        handler.ValidateToken(response.Token, validationParameters, out var validatedToken);

        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.SignatureAlgorithm);
    }

    [Fact]
    public void CreateToken_JwtContainsDeviceIdClaim()
    {
        var response = _service.CreateToken("device-xyz", "profile-abc");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        Assert.Contains(jwt.Claims, c => c.Type == FloodRushClaimTypes.DeviceId && c.Value == "device-xyz");
    }

    [Fact]
    public void CreateToken_JwtContainsProfileIdClaim()
    {
        var response = _service.CreateToken("device-xyz", "profile-123");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        Assert.Contains(jwt.Claims, c => c.Type == FloodRushClaimTypes.ProfileId && c.Value == "profile-123");
    }

    [Fact]
    public void CreateToken_JwtHeaderContainsKeyId()
    {
        var response = _service.CreateToken("device-xyz", "profile-abc");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);

        Assert.NotNull(jwt.Header.Kid);
        Assert.NotEmpty(jwt.Header.Kid);
    }
}
