namespace HexMaster.FloodRush.Game.Services;

public sealed class ApiBaseUrlProvider : IApiBaseUrlProvider
{
    // Manual override – useful when running the MAUI app standalone outside Aspire.
    private const string ManualOverrideVariable = "FLOODRUSH_API_BASE_URL";

    // Injected by Aspire when AddAndroidEmulator().WithReference(api, devTunnel) is used.
    // Points to the dev-tunnel public URL on Android, or to localhost on Windows.
    private const string AspireServiceDiscoveryVariable = "services__hexmaster-floodrush-api__https__0";

    // Android emulators reach the host at 10.0.2.2; port matches Aspire's HTTPS assignment.
    private static readonly Uri AndroidFallbackUri = new("https://10.0.2.2:7158/");
    private static readonly Uri DesktopFallbackUri = new("https://localhost:7158/");

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

        // Neither env var is set.  On Android, fall back to the host machine alias
        // (10.0.2.2) rather than localhost, which is unreachable inside the emulator.
        return OperatingSystem.IsAndroid() ? AndroidFallbackUri : DesktopFallbackUri;
    }

    public HttpMessageHandler CreateHandler()
    {
        var handler = new HttpClientHandler();

#if ANDROID && DEBUG
        // The ASP.NET Core dev cert is self-signed with CN=localhost.
        // When the Android emulator reaches it via 10.0.2.2 or a dev tunnel, Android's
        // trust store rejects it because the CN doesn't match and the CA is unknown.
        // Accept any certificate in debug builds so local dev sessions work without
        // manually installing the dev cert on each emulator.
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

        return handler;
    }

    /// <summary>
    /// Replaces localhost / 127.0.0.1 with 10.0.2.2 so the Android emulator
    /// can reach services running on the development machine.
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
