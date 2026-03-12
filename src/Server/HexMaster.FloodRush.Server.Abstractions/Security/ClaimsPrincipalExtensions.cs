using System.Security.Claims;

namespace HexMaster.FloodRush.Server.Abstractions.Security;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredDeviceId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(FloodRushClaimTypes.DeviceId)
        ?? throw new InvalidOperationException("Authenticated device is missing the device identifier claim.");

    public static string GetRequiredProfileId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(FloodRushClaimTypes.ProfileId)
        ?? throw new InvalidOperationException("Authenticated device is missing the profile identifier claim.");
}
