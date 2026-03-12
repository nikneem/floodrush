namespace HexMaster.FloodRush.Api.Authentication;

internal sealed record DeviceLoginResponse(
    string Token,
    DateTimeOffset ExpiresAtUtc,
    string TokenType,
    string DeviceId);
