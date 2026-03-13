using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class DeviceAuthenticationService : IDeviceAuthenticationService
{
    private const string DeviceIdPreferenceKey = "device_authentication_device_id";

    private readonly IApiBaseUrlProvider apiBaseUrlProvider;
    private readonly ILogger<DeviceAuthenticationService> logger;
    private readonly SemaphoreSlim gate = new(1, 1);

    private string? accessToken;
    private DateTimeOffset accessTokenExpiresAtUtc;

    public DeviceAuthenticationService(
        IApiBaseUrlProvider apiBaseUrlProvider,
        ILogger<DeviceAuthenticationService> logger)
    {
        this.apiBaseUrlProvider = apiBaseUrlProvider;
        this.logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasValidAccessToken())
        {
            logger.LogDebug("Reusing cached device access token.");
            return accessToken!;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (HasValidAccessToken())
            {
                logger.LogDebug("Reusing cached device access token after waiting for authentication gate.");
                return accessToken!;
            }

            using var activity = FloodRushTelemetry.ActivitySource.StartActivity("device-authentication.login", ActivityKind.Client);
            activity?.SetTag("server.address", apiBaseUrlProvider.GetBaseUri().Authority);

            FloodRushTelemetry.DeviceLoginRequests.Add(1);
            logger.LogInformation("Requesting a new device access token from the FloodRush API.");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var client = new HttpClient
                {
                    BaseAddress = apiBaseUrlProvider.GetBaseUri()
                };

                var response = await client.PostAsJsonAsync(
                    "api/profiles/device/login",
                    new DeviceLoginRequest(GetOrCreateDeviceId()),
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<DeviceLoginResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("The device login endpoint returned an empty response.");

                accessToken = payload.Token;
                accessTokenExpiresAtUtc = payload.ExpiresAtUtc;

                activity?.SetStatus(ActivityStatusCode.Ok);
                logger.LogInformation("Received a new device access token that expires at {ExpiresAtUtc}.", payload.ExpiresAtUtc);
                return accessToken;
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                logger.LogWarning(exception, "Device login failed.");
                throw;
            }
            finally
            {
                FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
                {
                    { "operation", "device-login" }
                });
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private bool HasValidAccessToken() =>
        !string.IsNullOrWhiteSpace(accessToken) &&
        accessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1);

    private static string GetOrCreateDeviceId()
    {
        var deviceId = Preferences.Default.Get<string?>(DeviceIdPreferenceKey, null);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        deviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdPreferenceKey, deviceId);
        return deviceId;
    }
}
