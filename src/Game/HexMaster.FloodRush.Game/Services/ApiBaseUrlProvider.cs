namespace HexMaster.FloodRush.Game.Services;

public sealed class ApiBaseUrlProvider : IApiBaseUrlProvider
{
    // Manual override – useful when running the MAUI app standalone outside Aspire.
    private const string ManualOverrideVariable = "FLOODRUSH_API_BASE_URL";

    // Injected by Aspire when AddAndroidEmulator().WithReference(api, devTunnel) is used.
    // Points to the dev-tunnel public URL on Android, or to localhost on Windows.
    private const string AspireServiceDiscoveryVariable = "services__hexmaster-floodrush-api__https__0";

    private static readonly Uri FallbackBaseUri = new("https://localhost:7158/");

    public Uri GetBaseUri()
    {
        // Aspire service-discovery takes priority: on Android it carries the dev-tunnel
        // URL (reachable by the emulator); on Windows it carries localhost.
        // The manual override is checked second for standalone / CI use.
        var rawUrl = Environment.GetEnvironmentVariable(AspireServiceDiscoveryVariable)
                  ?? Environment.GetEnvironmentVariable(ManualOverrideVariable);

        if (!string.IsNullOrWhiteSpace(rawUrl) &&
            Uri.TryCreate(rawUrl, UriKind.Absolute, out var configuredUri))
        {
            // Safety net: if somehow a localhost URL reaches an Android emulator,
            // substitute 10.0.2.2 (the host-machine alias inside Android emulators).
            var resolved = OperatingSystem.IsAndroid()
                ? MakeAndroidCompatible(configuredUri)
                : configuredUri;

            return EnsureTrailingSlash(resolved);
        }

        return FallbackBaseUri;
    }

    /// <summary>
    /// Replaces localhost / 127.0.0.1 with 10.0.2.2 so the Android emulator
    /// can reach services running on the development machine.
    /// With proper dev-tunnel configuration this substitution is never needed,
    /// but it acts as a last-resort fallback.
    /// </summary>
    private static Uri MakeAndroidCompatible(Uri uri)
    {
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host == "127.0.0.1")
        {
            return new UriBuilder(uri) { Host = "10.0.2.2" }.Uri;
        }

        return uri;
    }

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri($"{uri.AbsoluteUri}/", UriKind.Absolute);
}
