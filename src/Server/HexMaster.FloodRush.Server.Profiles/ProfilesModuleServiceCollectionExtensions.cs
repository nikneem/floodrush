using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Profiles.Authentication;
using HexMaster.FloodRush.Server.Profiles.Data;
using HexMaster.FloodRush.Server.Profiles.Features.DeviceLogin;
using HexMaster.FloodRush.Server.Profiles.Features.GetCurrentProfile;
using HexMaster.FloodRush.Server.Profiles.Features.UpdateProfile;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HexMaster.FloodRush.Server.Profiles;

public static class ProfilesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddProfilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DeviceTokenOptions>()
            .Bind(configuration.GetSection(DeviceTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static options => options.TokenLifetimeMinutes > 0,
                "Token lifetime must be greater than zero.")
            .Validate(
                static options => options.SigningKey.Length >= DeviceTokenOptions.MinimumSigningKeyLength,
                $"Signing key must be at least {DeviceTokenOptions.MinimumSigningKeyLength} characters long.")
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureDeviceJwtBearerOptions>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorization();

        services.AddSingleton<DeviceTokenService>();
        services.AddSingleton<IPlayerProfilesRepository, TablePlayerProfilesRepository>();

        services.AddScoped<ICommandHandler<LoginDeviceCommand, DeviceLoginResponse>, LoginDeviceCommandHandler>();
        services.AddScoped<IQueryHandler<GetCurrentProfileQuery, PlayerProfileDto>, GetCurrentProfileQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileCommand, PlayerProfileDto>, UpdateProfileCommandHandler>();

        return services;
    }
}
