using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Scores.Data;
using HexMaster.FloodRush.Server.Scores.Features.GetTopScores;
using HexMaster.FloodRush.Server.Scores.Features.SubmitScore;
using HexMaster.FloodRush.Shared.Contracts.Scores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HexMaster.FloodRush.Server.Scores;

public static class ScoresModuleServiceCollectionExtensions
{
    public static IServiceCollection AddScoresModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IScoresRepository, TableScoresRepository>();
        services.AddScoped<ICommandHandler<SubmitScoreCommand, LevelScoreDto>, SubmitScoreCommandHandler>();
        services.AddScoped<IQueryHandler<GetTopScoresQuery, TopScoresResponse>, GetTopScoresQueryHandler>();

        return services;
    }
}
