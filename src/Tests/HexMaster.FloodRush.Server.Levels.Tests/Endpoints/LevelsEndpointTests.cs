using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Levels.Features.GetLevelRevision;
using HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;
using HexMaster.FloodRush.Server.Levels.Features.SeedBasicLevels;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace HexMaster.FloodRush.Server.Levels.Tests.Endpoints;

public sealed class LevelsEndpointTests : IAsyncLifetime
{
    private WebApplication _app = default!;
    private HttpClient _client = default!;
    private readonly MutableLevelRevisionHandler _levelRevisionHandler = new();

    public async Task InitializeAsync()
    {
        _app = BuildApp("Development", _levelRevisionHandler);
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetReleasedLevels_AuthenticatedUser_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/levels/released");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLevelRevision_WhenFound_ReturnsOk()
    {
        _levelRevisionHandler.NextResult = new LevelRevisionDto(
            "level-001", "rev-1", "Level 1", "Easy", 10, 6, 3000, 3,
            [new LevelFixedTileDto(LevelFixedTileTypeDto.StartPoint, 0, 2, OutputDirection: BoardDirectionDto.Right)]);

        var response = await _client.GetAsync("/api/levels/level-001/revisions/rev-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLevelRevision_WhenNotFound_ReturnsNotFound()
    {
        _levelRevisionHandler.NextResult = null;

        var response = await _client.GetAsync("/api/levels/unknown/revisions/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SeedBasicLevels_InDevelopment_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/levels/dev/seed-basic-levels", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SeedBasicLevels_InProduction_ReturnsNotFound()
    {
        await using var app = BuildApp("Production", new MutableLevelRevisionHandler());
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/api/levels/dev/seed-basic-levels", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplication BuildApp(string environment, MutableLevelRevisionHandler levelRevisionHandler)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy<string>(RateLimitPolicies.General, _ => RateLimitPartition.GetNoLimiter("test"));
            options.AddPolicy<string>(RateLimitPolicies.DeviceLogin, _ => RateLimitPartition.GetNoLimiter("test"));
        });
        builder.Services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = "Test";
            o.DefaultChallengeScheme = "Test";
        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton<IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse>>(
            new StubReleasedLevelsHandler());
        builder.Services.AddSingleton<IQueryHandler<GetLevelRevisionQuery, LevelRevisionDto?>>(
            levelRevisionHandler);
        builder.Services.AddSingleton<ICommandHandler<SeedBasicLevelsCommand, SeedBasicLevelsResponse>>(
            new StubSeedCommandHandler());

        var app = builder.Build();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLevelsModule();
        return app;
    }
}

internal sealed class MutableLevelRevisionHandler : IQueryHandler<GetLevelRevisionQuery, LevelRevisionDto?>
{
    public LevelRevisionDto? NextResult { get; set; }

    public ValueTask<LevelRevisionDto?> HandleAsync(GetLevelRevisionQuery query, CancellationToken ct) =>
        ValueTask.FromResult(NextResult);
}

internal sealed class StubReleasedLevelsHandler : IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse>
{
    public ValueTask<ReleasedLevelsResponse> HandleAsync(GetReleasedLevelsQuery query, CancellationToken ct) =>
        ValueTask.FromResult(new ReleasedLevelsResponse([]));
}

internal sealed class StubSeedCommandHandler : ICommandHandler<SeedBasicLevelsCommand, SeedBasicLevelsResponse>
{
    public ValueTask<SeedBasicLevelsResponse> HandleAsync(SeedBasicLevelsCommand command, CancellationToken ct) =>
        ValueTask.FromResult(new SeedBasicLevelsResponse(1));
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(FloodRushClaimTypes.DeviceId, "test-device-id"),
            new Claim(FloodRushClaimTypes.ProfileId, "test-profile-id"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
