using HexMaster.FloodRush.Api.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services
    .AddOptions<DeviceTokenOptions>()
    .Bind(builder.Configuration.GetSection(DeviceTokenOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        static options => options.TokenLifetimeMinutes > 0,
        "Token lifetime must be greater than zero.")
    .Validate(
        static options => options.SigningKey.Length >= DeviceTokenOptions.MinimumSigningKeyLength,
        $"Signing key must be at least {DeviceTokenOptions.MinimumSigningKeyLength} characters long.")
    .ValidateOnStart();

builder.Services.AddSingleton<DeviceTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(static (options, serviceProvider) =>
    {
        var deviceTokenOptions = serviceProvider
            .GetRequiredService<IOptions<DeviceTokenOptions>>()
            .Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = deviceTokenOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = deviceTokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = DeviceTokenOptions.CreateSigningKey(deviceTokenOptions.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var authGroup = app.MapGroup("/api/auth/device").WithTags("Device Authentication");

authGroup.MapPost("/login", (
    DeviceLoginRequest request,
    DeviceTokenService tokenService) =>
{
    var validationErrors = DeviceLoginRequestValidator.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var response = tokenService.CreateToken(request.DeviceId.Trim());
    return Results.Ok(response);
})
.AllowAnonymous()
.WithName("DeviceLogin");

authGroup.MapGet("/me", (AuthenticatedDevice device) =>
    Results.Ok(new DeviceIdentityResponse(device.DeviceId)))
    .RequireAuthorization()
    .WithName("GetAuthenticatedDevice");

app.Run();
