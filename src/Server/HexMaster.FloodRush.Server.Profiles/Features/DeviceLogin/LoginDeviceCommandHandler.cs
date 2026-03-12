using System.Text.RegularExpressions;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Profiles.Authentication;
using HexMaster.FloodRush.Server.Profiles.Data;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Features.DeviceLogin;

internal sealed partial class LoginDeviceCommandHandler(
    IPlayerProfilesRepository repository,
    DeviceTokenService tokenService)
    : ICommandHandler<LoginDeviceCommand, DeviceLoginResponse>
{
    private const int MinimumDeviceIdLength = 8;
    private const int MaximumDeviceIdLength = 200;

    public async ValueTask<DeviceLoginResponse> HandleAsync(
        LoginDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var deviceId = NormalizeDeviceId(command.DeviceId);
        var profile = await repository.GetOrCreateByDeviceIdAsync(deviceId, cancellationToken);

        return tokenService.CreateToken(profile.DeviceId, profile.ProfileId);
    }

    private static string NormalizeDeviceId(string deviceId)
    {
        var normalized = deviceId.Trim();

        if (normalized.Length is < MinimumDeviceIdLength or > MaximumDeviceIdLength)
        {
            throw new ArgumentException(
                $"DeviceId must be between {MinimumDeviceIdLength} and {MaximumDeviceIdLength} characters.",
                nameof(deviceId));
        }

        if (!AllowedDeviceIdPattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "DeviceId may only contain letters, numbers, hyphens, underscores, periods, and colons.",
                nameof(deviceId));
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedDeviceIdPattern();
}
