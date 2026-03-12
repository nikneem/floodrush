using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Domain.Levels;

public sealed class LevelDefinition
{
    private readonly List<FixedTile> fixedTiles = [];
    private BoardDimensions boardDimensions;
    private FlowSpeedIndicator flowSpeedIndicator;

    public LevelDefinition(
        string levelId,
        BoardDimensions boardDimensions,
        int startDelayMilliseconds,
        FlowSpeedIndicator flowSpeedIndicator,
        IEnumerable<FixedTile> fixedTiles)
    {
        this.boardDimensions = Guard.AgainstNull(boardDimensions, nameof(boardDimensions)).Clone();
        this.flowSpeedIndicator = Guard.AgainstNull(flowSpeedIndicator, nameof(flowSpeedIndicator)).Clone();

        SetLevelId(levelId);
        StartDelayMilliseconds = Guard.AgainstNegative(startDelayMilliseconds, nameof(startDelayMilliseconds));
        SetFixedTiles(fixedTiles);
    }

    public string LevelId { get; private set; } = string.Empty;

    public BoardDimensions BoardDimensions
    {
        get => boardDimensions.Clone();
        private set => boardDimensions = value.Clone();
    }

    public int StartDelayMilliseconds { get; private set; }

    public FlowSpeedIndicator FlowSpeedIndicator
    {
        get => flowSpeedIndicator.Clone();
        private set => flowSpeedIndicator = value.Clone();
    }

    public IReadOnlyCollection<FixedTile> FixedTiles => fixedTiles.Select(static tile => tile.Clone()).ToArray();

    public void SetLevelId(string levelId) =>
        LevelId = Guard.AgainstNullOrWhiteSpace(levelId, nameof(levelId));

    public void SetBoardDimensions(BoardDimensions boardDimensions)
    {
        var candidateBoardDimensions = Guard.AgainstNull(boardDimensions, nameof(boardDimensions)).Clone();
        EnsureValidState(candidateBoardDimensions, flowSpeedIndicator, fixedTiles, StartDelayMilliseconds);
        BoardDimensions = candidateBoardDimensions;
    }

    public void SetStartDelayMilliseconds(int startDelayMilliseconds)
    {
        var candidateStartDelayMilliseconds =
            Guard.AgainstNegative(startDelayMilliseconds, nameof(startDelayMilliseconds));

        EnsureValidState(boardDimensions, flowSpeedIndicator, fixedTiles, candidateStartDelayMilliseconds);
        StartDelayMilliseconds = candidateStartDelayMilliseconds;
    }

    public void SetFlowSpeedIndicator(FlowSpeedIndicator flowSpeedIndicator)
    {
        var candidateFlowSpeedIndicator =
            Guard.AgainstNull(flowSpeedIndicator, nameof(flowSpeedIndicator)).Clone();

        EnsureValidState(boardDimensions, candidateFlowSpeedIndicator, fixedTiles, StartDelayMilliseconds);
        FlowSpeedIndicator = candidateFlowSpeedIndicator;
    }

    public void SetFixedTiles(IEnumerable<FixedTile> fixedTiles)
    {
        var candidateTiles = CloneTiles(fixedTiles);
        EnsureValidState(boardDimensions, flowSpeedIndicator, candidateTiles, StartDelayMilliseconds);

        this.fixedTiles.Clear();
        this.fixedTiles.AddRange(candidateTiles);
    }

    public void AddFixedTile(FixedTile fixedTile)
    {
        var candidateTiles = CloneTiles(fixedTiles);
        candidateTiles.Add(Guard.AgainstNull(fixedTile, nameof(fixedTile)).Clone());
        SetFixedTiles(candidateTiles);
    }

    private static List<FixedTile> CloneTiles(IEnumerable<FixedTile> fixedTiles)
    {
        ArgumentNullException.ThrowIfNull(fixedTiles);
        return [.. fixedTiles.Select(static tile => Guard.AgainstNull(tile, nameof(fixedTiles)).Clone())];
    }

    private void EnsureValidState(
        BoardDimensions candidateBoardDimensions,
        FlowSpeedIndicator candidateFlowSpeedIndicator,
        IReadOnlyCollection<FixedTile> candidateTiles,
        int candidateStartDelayMilliseconds)
    {
        Guard.AgainstNull(candidateBoardDimensions, nameof(candidateBoardDimensions));
        Guard.AgainstNull(candidateFlowSpeedIndicator, nameof(candidateFlowSpeedIndicator));
        Guard.AgainstNegative(candidateStartDelayMilliseconds, nameof(candidateStartDelayMilliseconds));

        if (candidateTiles.Count == 0)
        {
            throw new InvalidOperationException("A level must define at least one fixed tile.");
        }

        if (!candidateTiles.OfType<StartPointTile>().Any())
        {
            throw new InvalidOperationException("A level must define at least one start point.");
        }

        if (!candidateTiles.OfType<FinishPointTile>().Any())
        {
            throw new InvalidOperationException("A level must define at least one finish point.");
        }

        var occupiedPositions = new HashSet<GridPosition>();
        foreach (var tile in candidateTiles)
        {
            if (!candidateBoardDimensions.Contains(tile.Position))
            {
                throw new InvalidOperationException(
                    $"Tile '{tile.FixedTileType}' at ({tile.Position.X}, {tile.Position.Y}) is outside the board.");
            }

            if (!occupiedPositions.Add(tile.Position))
            {
                throw new InvalidOperationException(
                    $"Multiple fixed tiles cannot occupy ({tile.Position.X}, {tile.Position.Y}).");
            }
        }

        if (!AreAllFinishPointsReachable(candidateBoardDimensions, candidateTiles))
        {
            throw new InvalidOperationException(
                "All finish points must be theoretically reachable from at least one start point.");
        }
    }

    private static bool AreAllFinishPointsReachable(
        BoardDimensions boardDimensions,
        IReadOnlyCollection<FixedTile> fixedTiles)
    {
        var tileMap = fixedTiles.ToDictionary(static tile => tile.Position, static tile => tile);
        var startTiles = fixedTiles.OfType<StartPointTile>().ToArray();
        var finishPositions = fixedTiles.OfType<FinishPointTile>().Select(static tile => tile.Position).ToArray();

        var visited = new HashSet<GridPosition>();
        var queue = new Queue<GridPosition>();

        foreach (var startTile in startTiles)
        {
            if (visited.Add(startTile.Position))
            {
                queue.Enqueue(startTile.Position);
            }
        }

        while (queue.Count > 0)
        {
            var currentPosition = queue.Dequeue();
            tileMap.TryGetValue(currentPosition, out var currentTile);

            foreach (var direction in GetOutgoingDirections(currentTile))
            {
                GridPosition nextPosition;
                try
                {
                    nextPosition = currentPosition.Move(direction);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }

                if (!boardDimensions.Contains(nextPosition))
                {
                    continue;
                }

                tileMap.TryGetValue(nextPosition, out var nextTile);
                if (!CanAcceptIncoming(nextTile, direction.Opposite()))
                {
                    continue;
                }

                if (visited.Add(nextPosition))
                {
                    queue.Enqueue(nextPosition);
                }
            }
        }

        return finishPositions.All(visited.Contains);
    }

    private static IReadOnlyCollection<BoardDirection> GetOutgoingDirections(FixedTile? tile) =>
        tile?.GetOutgoingDirections() ?? BoardDirectionExtensions.AllDirections;

    private static bool CanAcceptIncoming(FixedTile? tile, BoardDirection incomingDirection) =>
        tile?.CanAcceptFlowFrom(incomingDirection) ?? true;
}
