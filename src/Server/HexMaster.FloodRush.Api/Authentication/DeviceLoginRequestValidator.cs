using System.Text.RegularExpressions;

namespace HexMaster.FloodRush.Api.Authentication;

internal static partial class DeviceLoginRequestValidator
{
    private const int MinimumDeviceIdLength = 8;
    private const int MaximumDeviceIdLength = 200;

    public static Dictionary<string, string[]> Validate(DeviceLoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var deviceId = request.DeviceId?.Trim();

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            errors[nameof(DeviceLoginRequest.DeviceId)] = ["DeviceId is required."];
            return errors;
        }

        if (deviceId.Length is < MinimumDeviceIdLength or > MaximumDeviceIdLength)
        {
            errors[nameof(DeviceLoginRequest.DeviceId)] =
            [
                $"DeviceId must be between {MinimumDeviceIdLength} and {MaximumDeviceIdLength} characters."
            ];
        }

        if (!AllowedDeviceIdPattern().IsMatch(deviceId))
        {
            errors[nameof(DeviceLoginRequest.DeviceId)] =
            [
                "DeviceId may only contain letters, numbers, hyphens, underscores, periods, and colons."
            ];
        }

        return errors;
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedDeviceIdPattern();
}
