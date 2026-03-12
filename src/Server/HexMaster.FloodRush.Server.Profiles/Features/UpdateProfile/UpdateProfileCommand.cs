using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Features.UpdateProfile;

public sealed record UpdateProfileCommand(string DeviceId, string DisplayName) : ICommand<PlayerProfileDto>;
