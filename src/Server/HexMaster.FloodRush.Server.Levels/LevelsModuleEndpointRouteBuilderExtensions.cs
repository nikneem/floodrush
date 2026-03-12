using System.Security.Claims;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        return endpoints;
    }
}
