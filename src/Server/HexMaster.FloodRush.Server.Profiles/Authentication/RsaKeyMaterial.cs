using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal sealed class RsaKeyMaterial : IDisposable
{
    public string KeyId { get; }
    public RsaSecurityKey SecurityKey { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public RsaKeyMaterial()
    {
        KeyId = Guid.NewGuid().ToString("N")[..16];
        var rsa = RSA.Create(2048);
        SecurityKey = new RsaSecurityKey(rsa) { KeyId = KeyId };
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool IsExpiredFor(int tokenLifetimeMinutes, int rotationIntervalMinutes)
    {
        var retentionPeriod = TimeSpan.FromMinutes(tokenLifetimeMinutes + rotationIntervalMinutes);
        return DateTimeOffset.UtcNow - CreatedAtUtc > retentionPeriod;
    }

    public void Dispose() => SecurityKey.Rsa?.Dispose();
}
