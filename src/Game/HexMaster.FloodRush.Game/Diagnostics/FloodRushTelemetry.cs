using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HexMaster.FloodRush.Game.Diagnostics;

public static class FloodRushTelemetry
{
    public const string ServiceName = "HexMaster.FloodRush.Game";
    public const string ActivitySourceName = ServiceName;
    public const string MeterName = ServiceName;

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);
    public static readonly Counter<long> NavigationRequests = Meter.CreateCounter<long>("floodrush.navigation.requests");
    public static readonly Counter<long> UserActions = Meter.CreateCounter<long>("floodrush.user.actions");
    public static readonly Counter<long> ApiRequests = Meter.CreateCounter<long>("floodrush.api.requests");
    public static readonly Counter<long> DeviceLoginRequests = Meter.CreateCounter<long>("floodrush.device-login.requests");
    public static readonly Counter<long> CacheOperations = Meter.CreateCounter<long>("floodrush.cache.operations");
    public static readonly Counter<long> SettingsChanges = Meter.CreateCounter<long>("floodrush.settings.changes");
    public static readonly Histogram<double> OperationDurationMs = Meter.CreateHistogram<double>("floodrush.operation.duration", unit: "ms");
}
