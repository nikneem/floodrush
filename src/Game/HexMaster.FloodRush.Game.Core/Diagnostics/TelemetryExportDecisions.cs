namespace HexMaster.FloodRush.Game.Core.Diagnostics;

public static class TelemetryExportDecisions
{
    public static bool ShouldEnableOtlpExport(string? otlpEndpoint) =>
        !string.IsNullOrWhiteSpace(otlpEndpoint);
}
