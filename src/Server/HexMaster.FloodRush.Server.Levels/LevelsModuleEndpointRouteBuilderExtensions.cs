using System.Security.Claims;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Levels.Features.GetLevelRevision;
using HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;
using HexMaster.FloodRush.Server.Levels.Features.SeedBasicLevels;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;

namespace HexMaster.FloodRush.Server.Levels;

public static class LevelsModuleEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLevelsModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/levels").WithTags("Levels");

        group.MapGet("/released", async (
            ClaimsPrincipal principal,
            IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new GetReleasedLevelsQuery(principal.GetRequiredProfileId()),
                cancellationToken);

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("Levels_GetReleased");

        group.MapGet("/{levelId}/revisions/{revision}", async (
            string levelId,
            string revision,
            ClaimsPrincipal principal,
            IQueryHandler<GetLevelRevisionQuery, LevelRevisionDto?> handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new GetLevelRevisionQuery(principal.GetRequiredProfileId(), levelId, revision),
                cancellationToken);

            return response is null ? Results.NotFound() : Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("Levels_GetRevision");

        group.MapPost("/dev/seed-basic-levels", async (
            IHostEnvironment environment,
            ICommandHandler<SeedBasicLevelsCommand, SeedBasicLevelsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            if (!environment.IsDevelopment())
            {
                return Results.NotFound();
            }

            var response = await handler.HandleAsync(new SeedBasicLevelsCommand(), cancellationToken);
            return Results.Ok(response);
        })
        .AllowAnonymous()
        .WithName("Levels_SeedBasicLevels");

        return endpoints;
    }
}
