using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Profiles.Data;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Features.GetCurrentProfile;

internal sealed class GetCurrentProfileQueryHandler(IPlayerProfilesRepository repository)
    : IQueryHandler<GetCurrentProfileQuery, PlayerProfileDto>
{
    public async ValueTask<PlayerProfileDto> HandleAsync(
        GetCurrentProfileQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetByDeviceIdAsync(query.DeviceId, cancellationToken);

        return profile ?? throw new InvalidOperationException(
            $"Profile for device '{query.DeviceId}' does not exist.");
    }
}
