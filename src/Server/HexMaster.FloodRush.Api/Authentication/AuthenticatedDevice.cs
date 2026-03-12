using System.Security.Claims;

namespace HexMaster.FloodRush.Api.Authentication;

internal sealed class AuthenticatedDevice
{
    public AuthenticatedDevice(ClaimsPrincipal principal)
    {
        DeviceId = principal.FindFirstValue(DeviceTokenService.DeviceIdClaimType)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated device is missing the device identifier claim.");
    }

    public string DeviceId { get; }
}
