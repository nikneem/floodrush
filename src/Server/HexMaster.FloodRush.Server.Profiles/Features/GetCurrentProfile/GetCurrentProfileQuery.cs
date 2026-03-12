using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Features.GetCurrentProfile;

public sealed record GetCurrentProfileQuery(string DeviceId) : IQuery<PlayerProfileDto>;
