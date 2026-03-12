using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HexMaster.FloodRush.Server.Profiles.Authentication;

internal sealed class ConfigureDeviceJwtBearerOptions(IOptions<DeviceTokenOptions> tokenOptions)
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
            IssuerSigningKey = DeviceTokenOptions.CreateSigningKey(deviceTokenOptions.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
