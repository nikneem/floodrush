using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal sealed class RotatingRsaKeyProvider
    : ITokenSigningKeyProvider, IHostedService, IDisposable
{
    private readonly List<RsaKeyMaterial> _keys = [];
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly DeviceTokenOptions _options;
    private readonly ILogger<RotatingRsaKeyProvider> _logger;
    private Timer? _rotationTimer;

    public RotatingRsaKeyProvider(
        IOptions<DeviceTokenOptions> options,
        ILogger<RotatingRsaKeyProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RotateKey();

        var interval = TimeSpan.FromMinutes(_options.KeyRotationIntervalMinutes);
        _rotationTimer = new Timer(_ => RotateKey(), null, interval, interval);

        _logger.LogInformation(
            "RSA key rotation started. Initial key id: {KeyId}. Rotation interval: {IntervalMinutes} minutes.",
            GetCurrentKey().KeyId,
            _options.KeyRotationIntervalMinutes);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _rotationTimer?.Change(Timeout.Infinite, 0);
        _logger.LogInformation("RSA key rotation stopped.");
        return Task.CompletedTask;
    }

    public SigningCredentials GetCurrentSigningCredentials()
    {
        var key = GetCurrentKey();
        return new SigningCredentials(key.SecurityKey, SecurityAlgorithms.RsaSha256);
    }

    public IEnumerable<SecurityKey> GetAllValidationKeys()
    {
        _lock.EnterReadLock();
        try
        {
            return _keys.Select(k => k.SecurityKey).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public JsonWebKeySet GetPublicKeySet()
    {
        _lock.EnterReadLock();
        try
        {
            var keySet = new JsonWebKeySet();
            foreach (var key in _keys)
            {
                var publicParams = key.SecurityKey.Rsa.ExportParameters(includePrivateParameters: false);
                var publicRsa = RSA.Create();
                publicRsa.ImportParameters(publicParams);
                var publicSecurityKey = new RsaSecurityKey(publicRsa) { KeyId = key.KeyId };

                var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicSecurityKey);
                jwk.Use = "sig";
                keySet.Keys.Add(jwk);
            }
            return keySet;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private RsaKeyMaterial GetCurrentKey()
    {
        _lock.EnterReadLock();
        try
        {
            return _keys[^1];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void RotateKey()
    {
        var newKey = new RsaKeyMaterial();

        _lock.EnterWriteLock();
        try
        {
            _keys.Add(newKey);

            var expiredKeys = _keys
                .Where(k => k.IsExpiredFor(_options.TokenLifetimeMinutes, _options.KeyRotationIntervalMinutes))
                .ToList();

            foreach (var expired in expiredKeys)
            {
                _keys.Remove(expired);
                expired.Dispose();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _logger.LogInformation(
            "RSA signing key rotated. New key id: {KeyId}. Active keys: {ActiveKeyCount}.",
            newKey.KeyId,
            _keys.Count);
    }

    public void Dispose()
    {
        _rotationTimer?.Dispose();
        _lock.EnterWriteLock();
        try
        {
            foreach (var key in _keys)
            {
                key.Dispose();
            }
            _keys.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        _lock.Dispose();
    }
}
