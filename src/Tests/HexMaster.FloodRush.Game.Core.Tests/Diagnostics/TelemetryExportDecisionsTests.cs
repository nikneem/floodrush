using HexMaster.FloodRush.Game.Core.Diagnostics;

namespace HexMaster.FloodRush.Game.Core.Tests.Diagnostics;

public sealed class TelemetryExportDecisionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ShouldEnableOtlpExport_ReturnsFalse_WhenEndpointIsMissing(string? endpoint)
    {
        var result = TelemetryExportDecisions.ShouldEnableOtlpExport(endpoint);

        Assert.False(result);
    }

    [Theory]
    [InlineData("http://localhost:4317")]
    [InlineData("https://127.0.0.1:18889")]
    public void ShouldEnableOtlpExport_ReturnsTrue_WhenEndpointIsConfigured(string endpoint)
    {
        var result = TelemetryExportDecisions.ShouldEnableOtlpExport(endpoint);

        Assert.True(result);
    }
}
