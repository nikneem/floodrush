using System.Security.Claims;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Profiles.Features.DeviceLogin;
using HexMaster.FloodRush.Server.Profiles.Features.GetCurrentProfile;
using HexMaster.FloodRush.Server.Profiles.Features.UpdateProfile;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HexMaster.FloodRush.Server.Profiles;

public static class ProfilesModuleEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProfilesModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/profiles").WithTags("Profiles");

        group.MapPost("/device/login", async Task<IResult> (
            DeviceLoginRequest request,
            ICommandHandler<LoginDeviceCommand, DeviceLoginResponse> handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.DeviceId)] = ["DeviceId is required."]
                });
            }

            try
            {
                var response = await handler.HandleAsync(new LoginDeviceCommand(request.DeviceId), cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.DeviceId)] = [exception.Message]
                });
            }
        })
        .AllowAnonymous()
        .WithName("Profiles_DeviceLogin");

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            IQueryHandler<GetCurrentProfileQuery, PlayerProfileDto> handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                new GetCurrentProfileQuery(principal.GetRequiredDeviceId()),
                cancellationToken);

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("Profiles_GetCurrentProfile");

        group.MapPut("/me", async Task<IResult> (
            ClaimsPrincipal principal,
            UpdatePlayerProfileRequest request,
            ICommandHandler<UpdateProfileCommand, PlayerProfileDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(
                    new UpdateProfileCommand(principal.GetRequiredDeviceId(), request.DisplayName),
                    cancellationToken);

                return Results.Ok(response);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.DisplayName)] = [exception.Message]
                });
            }
        })
        .RequireAuthorization()
        .WithName("Profiles_UpdateProfile");

        return endpoints;
    }
}
