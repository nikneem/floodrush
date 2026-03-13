using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var tables = builder.AddAzureStorage("floodrushstorage")
    .RunAsEmulator()
    .AddTables("floodrushtables");

var api = builder.AddProject<Projects.HexMaster_FloodRush_Api>("hexmaster-floodrush-api")
    .WithReference(tables)
    .WaitFor(tables)
    .WithSeedBasicLevelsCommand()
    .WithOtlpExporter();

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

var mauiApp = builder.AddMauiProject(
    "hexmaster-floodrush-game",
    @"..\..\..\Game\HexMaster.FloodRush.Game\HexMaster.FloodRush.Game.csproj");

// WithReference(api, publicDevTunnel) tells Aspire to inject the tunnel URL
// (not localhost) into the service-discovery env var that ApiBaseUrlProvider reads.
// WithOtlpDevTunnel() creates a separate tunnel for OpenTelemetry traffic so
// telemetry from the emulator reaches the Aspire dashboard.
mauiApp.AddAndroidEmulator()
    .WithOtlpDevTunnel()
    .WithReference(api, publicDevTunnel);

builder.Build().Run();
