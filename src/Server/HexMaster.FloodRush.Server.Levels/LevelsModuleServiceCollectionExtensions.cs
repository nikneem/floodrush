using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Server.Levels.Features.GetLevelRevision;
using HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;
using HexMaster.FloodRush.Server.Levels.Features.SeedBasicLevels;
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
        services.AddSingleton<BuiltInLevelsCatalog>();
        services.AddSingleton<BasicLevelsSeedCatalog>();
        services.AddSingleton<ILevelsRepository, TableLevelsRepository>();
        services.AddScoped<ICommandHandler<SeedBasicLevelsCommand, SeedBasicLevelsResponse>, SeedBasicLevelsCommandHandler>();
        services.AddScoped<IQueryHandler<GetLevelRevisionQuery, LevelRevisionDto?>, GetLevelRevisionQueryHandler>();
        services.AddScoped<IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse>, GetReleasedLevelsQueryHandler>();

        return services;
    }
}
