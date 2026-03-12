using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal sealed class DeviceTokenService(IOptions<DeviceTokenOptions> options)
{
    private readonly DeviceTokenOptions tokenOptions = options.Value;
    private readonly JwtSecurityTokenHandler tokenHandler = new();

    public DeviceLoginResponse CreateToken(string deviceId, string profileId)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(tokenOptions.TokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, deviceId),
            new Claim(FloodRushClaimTypes.DeviceId, deviceId),
            new Claim(FloodRushClaimTypes.ProfileId, profileId),
            new Claim(ClaimTypes.NameIdentifier, profileId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = tokenOptions.Issuer,
            Audience = tokenOptions.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                DeviceTokenOptions.CreateSigningKey(tokenOptions.SigningKey),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);

        return new DeviceLoginResponse(
            tokenHandler.WriteToken(token),
            expiresAt,
            "Bearer",
            deviceId,
            profileId);
    }
}
