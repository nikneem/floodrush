namespace HexMaster.FloodRush.Shared.Contracts.Profiles;

public sealed record PlayerProfileDto(
    string ProfileId,
    string DeviceId,
    string? DisplayName,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc);
