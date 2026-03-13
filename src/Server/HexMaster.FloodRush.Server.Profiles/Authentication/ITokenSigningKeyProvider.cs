using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal interface ITokenSigningKeyProvider
{
    SigningCredentials GetCurrentSigningCredentials();
    IEnumerable<SecurityKey> GetAllValidationKeys();
    JsonWebKeySet GetPublicKeySet();
}
