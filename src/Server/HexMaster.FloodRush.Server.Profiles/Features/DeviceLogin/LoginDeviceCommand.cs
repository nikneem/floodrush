using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Features.DeviceLogin;

public sealed record LoginDeviceCommand(string DeviceId) : ICommand<DeviceLoginResponse>;
