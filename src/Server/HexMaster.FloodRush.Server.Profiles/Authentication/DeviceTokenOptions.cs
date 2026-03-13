using System.ComponentModel.DataAnnotations;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal sealed class DeviceTokenOptions
{
    public const string SectionName = "Authentication:DeviceJwt";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 60 * 24 * 30)]
    public int TokenLifetimeMinutes { get; init; } = 60 * 24;

    [Range(1, 60 * 24 * 30)]
    public int KeyRotationIntervalMinutes { get; init; } = 60 * 24;
}
