using System.ComponentModel.DataAnnotations;

namespace HexMaster.FloodRush.Api.Authentication;

internal sealed record DeviceLoginRequest(
    [property: Required(AllowEmptyStrings = false)]
    string DeviceId);
