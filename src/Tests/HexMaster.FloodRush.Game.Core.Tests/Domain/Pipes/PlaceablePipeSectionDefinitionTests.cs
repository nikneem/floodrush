using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Pipes;

public sealed class PlaceablePipeSectionDefinitionTests
{
    [Fact]
    public void CreateRequiredSections_ReturnsAllSevenRequiredPipeTypes()
    {
        var sections = PlaceablePipeSectionDefinition.CreateRequiredSections();

        Assert.Equal(7, sections.Count);
        Assert.Equal(7, sections.Select(static section => section.PipeSectionType).Distinct().Count());
    }

    [Fact]
    public void NonCrossSection_RejectsSecondaryTraversalBonus()
    {
        Assert.Throws<ArgumentException>(() => new PlaceablePipeSectionDefinition(PipeSectionType.Horizontal, 5, 1));
    }

    [Fact]
    public void CrossSection_SupportsBothTraversalAxes()
    {
        var definition = new PlaceablePipeSectionDefinition(PipeSectionType.Cross, 10, 3);

        Assert.True(definition.SupportsAxis(FlowTraversalAxis.Horizontal));
        Assert.True(definition.SupportsAxis(FlowTraversalAxis.Vertical));
        Assert.Equal(3, definition.SecondaryTraversalBonusPoints);
    }
}
