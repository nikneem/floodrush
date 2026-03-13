using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Set UseAndroidEmulator=true in appsettings.Development.json (or via --UseAndroidEmulator=true
// on the command line) to target the Android emulator instead of the Windows client.
var useAndroidEmulator = builder.Configuration.GetValue<bool>("UseAndroidEmulator", false);

var tables = builder.AddAzureStorage("floodrushstorage")
    .RunAsEmulator()
    .AddTables("floodrushtables");

var api = builder.AddProject<Projects.HexMaster_FloodRush_Api>("hexmaster-floodrush-api")
    .WithReference(tables)
    .WaitFor(tables)
    .WithSeedBasicLevelsCommand()
    .WithOtlpExporter();

var mauiApp = builder.AddMauiProject(
    "hexmaster-floodrush-game",
    @"..\..\..\Game\HexMaster.FloodRush.Game\HexMaster.FloodRush.Game.csproj");

if (useAndroidEmulator)
{
    // Android emulators cannot reach localhost on the host machine directly.
    // A public dev tunnel bridges that gap: it exposes the API over a public HTTPS URL
    // that the emulator can reach.  Anonymous access is required so no sign-in prompt
    // appears on the device.
    //
    // Prerequisites:
    //   1. Install the devtunnel CLI  (winget install Microsoft.devtunnel)
    //   2. Sign in once:              devtunnel user login
    //
    // NOTE: The MAUI app does NOT start automatically.  After Aspire launches, open the
    // dashboard and click "Start" next to the hexmaster-floodrush-game resource.
    var publicDevTunnel = builder.AddDevTunnel("devtunnel-public")
        .WithAnonymousAccess()
        .WithReference(api.GetEndpoint("https"));

    // WithReference(api, publicDevTunnel) tells Aspire to inject the tunnel URL
    // (not localhost) into the service-discovery env var that ApiBaseUrlProvider reads.
    // WithOtlpDevTunnel() creates a separate tunnel for OpenTelemetry traffic so
    // telemetry from the emulator reaches the Aspire dashboard.
    mauiApp.AddAndroidEmulator()
        .WithOtlpDevTunnel()
        .WithReference(api, publicDevTunnel);
}
else
{
    // Default: run the Windows client. The API is reachable via localhost so no
    // dev tunnel is needed. OTLP traffic from the Windows process also reaches
    // the Aspire dashboard directly via localhost.
    mauiApp.AddWindowsDevice()
        .WithOtlpExporter()
        .WithReference(api);
}

builder.Build().Run();
