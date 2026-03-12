using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Data;

internal interface IPlayerProfilesRepository
{
    ValueTask<PlayerProfileDto> GetOrCreateByDeviceIdAsync(string deviceId, CancellationToken cancellationToken);

    ValueTask<PlayerProfileDto?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken);

    ValueTask<PlayerProfileDto> UpdateDisplayNameAsync(
        string deviceId,
        string displayName,
        CancellationToken cancellationToken);
}
