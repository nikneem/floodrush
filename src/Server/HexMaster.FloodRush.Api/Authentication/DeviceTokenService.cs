using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Api.Authentication;

internal sealed class DeviceTokenService
{
    public const string DeviceIdClaimType = "device_id";

    private readonly DeviceTokenOptions options;
    private readonly JwtSecurityTokenHandler tokenHandler = new();

    public DeviceTokenService(IOptions<DeviceTokenOptions> options)
    {
        this.options = options.Value;
    }

    public DeviceLoginResponse CreateToken(string deviceId)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(options.TokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, deviceId),
            new Claim(ClaimTypes.NameIdentifier, deviceId),
            new Claim(DeviceIdClaimType, deviceId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                DeviceTokenOptions.CreateSigningKey(options.SigningKey),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var serializedToken = tokenHandler.WriteToken(token);

        return new DeviceLoginResponse(serializedToken, expiresAt, "Bearer", deviceId);
    }
}
