namespace HexMaster.FloodRush.Shared.Contracts.Profiles;

public sealed record DeviceLoginResponse(
    string Token,
    DateTimeOffset ExpiresAtUtc,
    string TokenType,
    string DeviceId,
    string ProfileId);
