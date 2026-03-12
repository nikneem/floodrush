using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

/// <summary>
/// Combines a level's fixed tiles with player-placed pipes into a unified board
/// that the simulation engine can query for any grid position.
/// </summary>
public sealed class GameBoard
{
    private readonly Dictionary<GridPosition, FixedTile> fixedTileMap;
    private readonly Dictionary<GridPosition, PlacedPipe> placedPipes = [];
    private readonly BoardDimensions boardDimensions;

    private GameBoard(LevelDefinition level)
    {
        Guard.AgainstNull(level, nameof(level));
        fixedTileMap = level.FixedTiles.ToDictionary(t => t.Position);
        boardDimensions = level.BoardDimensions;
    }

    /// <summary>Read-only view of all player-placed pipes keyed by position.</summary>
    public IReadOnlyDictionary<GridPosition, PlacedPipe> PlacedPipes => placedPipes;

    /// <summary>Returns the fixed tile at <paramref name="position"/>, or null if none.</summary>
    public FixedTile? GetFixedTile(GridPosition position) =>
        fixedTileMap.TryGetValue(position, out var tile) ? tile : null;

    /// <summary>Returns the player-placed pipe at <paramref name="position"/>, or null if none.</summary>
    public PlacedPipe? GetPlacedPipe(GridPosition position) =>
        placedPipes.TryGetValue(position, out var pipe) ? pipe : null;

    /// <summary>Returns true when <paramref name="position"/> is within the board boundaries.</summary>
    public bool IsWithinBounds(GridPosition position) =>
        boardDimensions.Contains(position);

    /// <summary>Places or replaces a pipe at its position.</summary>
    internal void PlacePipe(PlacedPipe pipe) => placedPipes[pipe.Position] = pipe;

    /// <summary>Removes a previously placed pipe (no-op if no pipe exists at that position).</summary>
    internal void RemovePipe(GridPosition position) => placedPipes.Remove(position);

    /// <summary>Creates a <see cref="GameBoard"/> backed by the given level definition.</summary>
    public static GameBoard FromLevel(LevelDefinition level) => new(level);
}
