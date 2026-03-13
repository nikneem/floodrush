namespace HexMaster.FloodRush.Game.Services;

public interface INetworkStatusService
{
    bool HasInternetAccess { get; }
}
