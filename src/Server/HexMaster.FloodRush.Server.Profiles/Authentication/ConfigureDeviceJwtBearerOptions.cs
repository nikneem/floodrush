using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal sealed class ConfigureDeviceJwtBearerOptions(
    IOptions<DeviceTokenOptions> tokenOptions,
    ITokenSigningKeyProvider signingKeyProvider)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) =>
        Configure(Options.DefaultName, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.IsNullOrEmpty(name) && name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var deviceTokenOptions = tokenOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = deviceTokenOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = deviceTokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, _) =>
                signingKeyProvider.GetAllValidationKeys()
                    .Where(k => kid == null || k.KeyId == kid),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
