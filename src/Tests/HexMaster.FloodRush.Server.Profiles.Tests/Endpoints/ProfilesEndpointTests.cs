using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Abstractions.Security;
using HexMaster.FloodRush.Server.Profiles.Authentication;
using HexMaster.FloodRush.Server.Profiles.Features.DeviceLogin;
using HexMaster.FloodRush.Server.Profiles.Features.GetCurrentProfile;
using HexMaster.FloodRush.Server.Profiles.Features.UpdateProfile;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

namespace HexMaster.FloodRush.Server.Profiles.Tests.Endpoints;

public sealed class ProfilesEndpointTests : IAsyncLifetime
{
    private WebApplication _app = default!;
    private HttpClient _client = default!;
    private readonly MutableLoginHandler _loginHandler = new();
    private readonly MutableUpdateProfileHandler _updateHandler = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
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
        }).AddScheme<AuthenticationSchemeOptions, ProfilesTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton<ITokenSigningKeyProvider>(new StubKeyProvider());
        builder.Services.AddSingleton<ICommandHandler<LoginDeviceCommand, DeviceLoginResponse>>(_loginHandler);
        builder.Services.AddSingleton<IQueryHandler<GetCurrentProfileQuery, PlayerProfileDto>>(
            new StubGetProfileHandler());
        builder.Services.AddSingleton<ICommandHandler<UpdateProfileCommand, PlayerProfileDto>>(_updateHandler);

        _app = builder.Build();
        _app.UseRouting();
        _app.UseRateLimiter();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapProfilesModule();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetJwks_ReturnsOk()
    {
        var response = await _client.GetAsync("/.well-known/jwks.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeviceLogin_EmptyDeviceId_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/profiles/device/login",
            new { DeviceId = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeviceLogin_ValidDeviceId_ReturnsOk()
    {
        _loginHandler.Result = new DeviceLoginResponse("jwt-token", DateTimeOffset.UtcNow.AddHours(1), "Bearer", "device-12345678", "profile-1");

        var response = await _client.PostAsJsonAsync("/api/profiles/device/login",
            new { DeviceId = "device-12345678" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeviceLogin_HandlerThrowsArgumentException_ReturnsValidationProblem()
    {
        _loginHandler.ShouldThrow = true;

        var response = await _client.PostAsJsonAsync("/api/profiles/device/login",
            new { DeviceId = "device-12345678" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentProfile_AuthenticatedUser_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/profiles/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ValidDisplayName_ReturnsOk()
    {
        _updateHandler.Result = new PlayerProfileDto("p", "d", "New Name", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var response = await _client.PutAsJsonAsync("/api/profiles/me",
            new { DisplayName = "New Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_HandlerThrowsArgumentException_ReturnsValidationProblem()
    {
        _updateHandler.ShouldThrow = true;

        var response = await _client.PutAsJsonAsync("/api/profiles/me",
            new { DisplayName = new string('x', 200) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

internal sealed class MutableLoginHandler : ICommandHandler<LoginDeviceCommand, DeviceLoginResponse>
{
    public DeviceLoginResponse? Result { get; set; }
    public bool ShouldThrow { get; set; }

    public ValueTask<DeviceLoginResponse> HandleAsync(LoginDeviceCommand command, CancellationToken ct)
    {
        if (ShouldThrow) throw new ArgumentException("Invalid device ID format.");
        return ValueTask.FromResult(Result ?? new DeviceLoginResponse("token", DateTimeOffset.UtcNow.AddHours(1), "Bearer", command.DeviceId, "profile-1"));
    }
}

internal sealed class StubGetProfileHandler : IQueryHandler<GetCurrentProfileQuery, PlayerProfileDto>
{
    public ValueTask<PlayerProfileDto> HandleAsync(GetCurrentProfileQuery query, CancellationToken ct) =>
        ValueTask.FromResult(new PlayerProfileDto("profile-1", query.DeviceId, "Player", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
}

internal sealed class MutableUpdateProfileHandler : ICommandHandler<UpdateProfileCommand, PlayerProfileDto>
{
    public PlayerProfileDto? Result { get; set; }
    public bool ShouldThrow { get; set; }

    public ValueTask<PlayerProfileDto> HandleAsync(UpdateProfileCommand command, CancellationToken ct)
    {
        if (ShouldThrow) throw new ArgumentException("Display name is too long.");
        return ValueTask.FromResult(Result ?? new PlayerProfileDto("p", "d", command.DisplayName, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }
}

internal sealed class StubKeyProvider : ITokenSigningKeyProvider
{
    private readonly RsaKeyMaterial _key = new();

    public SigningCredentials GetCurrentSigningCredentials() =>
        new(_key.SecurityKey, SecurityAlgorithms.RsaSha256);

    public IEnumerable<SecurityKey> GetAllValidationKeys() => [_key.SecurityKey];

    public JsonWebKeySet GetPublicKeySet() => new();
}

internal sealed class ProfilesTestAuthHandler(
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
