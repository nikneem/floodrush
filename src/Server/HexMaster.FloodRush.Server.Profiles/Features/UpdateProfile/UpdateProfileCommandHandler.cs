using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Profiles.Data;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Features.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(IPlayerProfilesRepository repository)
    : ICommandHandler<UpdateProfileCommand, PlayerProfileDto>
{
    private const int MaximumDisplayNameLength = 50;

    public async ValueTask<PlayerProfileDto> HandleAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var displayName = command.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName is required.", nameof(command.DisplayName));
        }

        if (displayName.Length > MaximumDisplayNameLength)
        {
            throw new ArgumentException(
                $"DisplayName must be {MaximumDisplayNameLength} characters or fewer.",
                nameof(command.DisplayName));
        }

        return await repository.UpdateDisplayNameAsync(command.DeviceId, displayName, cancellationToken);
    }
}
