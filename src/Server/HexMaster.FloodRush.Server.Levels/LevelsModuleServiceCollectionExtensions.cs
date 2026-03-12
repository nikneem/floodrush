using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HexMaster.FloodRush.Server.Levels;

public static class LevelsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddLevelsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ILevelsRepository, TableLevelsRepository>();
        services.AddScoped<IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse>, GetReleasedLevelsQueryHandler>();

        return services;
    }
}
