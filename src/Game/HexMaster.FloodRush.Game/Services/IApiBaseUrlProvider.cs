namespace HexMaster.FloodRush.Game.Services;

public interface IApiBaseUrlProvider
{
    Uri GetBaseUri();

    /// <summary>
    /// Returns an <see cref="HttpMessageHandler"/> suitable for the current platform and build.
    /// On Android debug builds the handler bypasses TLS certificate validation so that the
    /// ASP.NET Core dev cert (which is self-signed and has a "localhost" CN) is accepted
    /// when the emulator connects via the 10.0.2.2 host alias or a dev tunnel.
    /// </summary>
    HttpMessageHandler CreateHandler();
}
