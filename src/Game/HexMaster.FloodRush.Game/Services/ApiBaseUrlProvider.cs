namespace HexMaster.FloodRush.Game.Services;

public sealed class ApiBaseUrlProvider : IApiBaseUrlProvider
{
    private const string ApiBaseUrlEnvironmentVariable = "FLOODRUSH_API_BASE_URL";
    private static readonly Uri FallbackBaseUri = new("https://localhost:7158/");

    public Uri GetBaseUri()
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) &&
            Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configuredUri))
        {
            return EnsureTrailingSlash(configuredUri);
        }

        return FallbackBaseUri;
    }

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri($"{uri.AbsoluteUri}/", UriKind.Absolute);
}
