namespace HexMaster.FloodRush.Game.Services;

public interface IDeviceAuthenticationService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
