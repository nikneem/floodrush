namespace HexMaster.FloodRush.Game.Services;

public sealed class NetworkStatusService : INetworkStatusService
{
    public bool HasInternetAccess =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
