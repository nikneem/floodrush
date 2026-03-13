using System.Security.Claims;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Scores.Features.GetPlayerBestScore;
using HexMaster.FloodRush.Server.Scores.Features.GetTopScores;
using HexMaster.FloodRush.Server.Scores.Features.SubmitScore;
using HexMaster.FloodRush.Shared.Contracts.Scores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HexMaster.FloodRush.Server.Scores;

public static class ScoresModuleEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapScoresModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/scores").WithTags("Scores");

        group.MapPost("/", async Task<IResult> (
            ClaimsPrincipal principal,
            SubmitScoreRequest request,
            ICommandHandler<SubmitScoreCommand, LevelScoreDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(
                    new SubmitScoreCommand(principal.GetRequiredProfileId(), request),
                    cancellationToken);

                return Results.Ok(response);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [exception.Message]
                });
            }
        })
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicies.General)
        .WithName("Scores_Submit");

        group.MapGet("/top/{levelId}", async (
            string levelId,
            int? take,
            IQueryHandler<GetTopScoresQuery, TopScoresResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new GetTopScoresQuery(levelId, Math.Clamp(take ?? 10, 1, 100)),
                cancellationToken);

            return Results.Ok(response);
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.General)
        .WithName("Scores_GetTop");

        group.MapGet("/my/{levelId}", async (
            string levelId,
            ClaimsPrincipal principal,
            IQueryHandler<GetPlayerBestScoreQuery, LevelScoreDto?> handler,
            CancellationToken cancellationToken) =>
        {
            var profileId = principal.GetRequiredProfileId();
            var result = await handler.HandleAsync(
                new GetPlayerBestScoreQuery(profileId, levelId),
                cancellationToken);

            return result is null ? Results.NoContent() : Results.Ok(result);
        })
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicies.General)
        .WithName("Scores_GetMyBest");

        return endpoints;
    }
}
