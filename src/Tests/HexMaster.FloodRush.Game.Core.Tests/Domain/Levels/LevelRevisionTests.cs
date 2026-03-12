using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Levels;

public sealed class LevelRevisionTests
{
    [Fact]
    public void Constructor_StoresComponents()
    {
        var token = LevelRevisionToken.New();
        var definition = CreateValidDefinition();
        var metadata = new LevelMetadata("Test Level", DifficultyLabel.Easy);

        var revision = new LevelRevision(token, definition, metadata);

        Assert.Same(token, revision.RevisionToken);
        Assert.Same(definition, revision.Definition);
        Assert.Same(metadata, revision.Metadata);
    }

    [Fact]
    public void Constructor_RejectsNullToken()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LevelRevision(null!, CreateValidDefinition(), new LevelMetadata("L")));
    }

    [Fact]
    public void Constructor_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LevelRevision(LevelRevisionToken.New(), null!, new LevelMetadata("L")));
    }

    [Fact]
    public void Constructor_RejectsNullMetadata()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LevelRevision(LevelRevisionToken.New(), CreateValidDefinition(), null!));
    }

    private static LevelDefinition CreateValidDefinition() =>
        new(
            "lv-1",
            "Level One",
            new BoardDimensions(4, 2),
            1000,
            new FlowSpeedIndicator(50),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);
}
