using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Api.Authentication;

internal sealed class DeviceTokenOptions
{
    public const string SectionName = "Authentication:DeviceJwt";
    public const int MinimumSigningKeyLength = 32;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 60 * 24 * 30)]
    public int TokenLifetimeMinutes { get; init; } = 60 * 24;

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
        new(Encoding.UTF8.GetBytes(signingKey));
}
