using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;
using HexMaster.FloodRush.Game.Core.Presentation.Gameplay;
using HexMaster.FloodRush.Game.Core.Presentation.Viewports;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Game.Services;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.ViewModels;

[QueryProperty(nameof(LevelId), "levelId")]
public sealed class GameplayViewModel : BaseViewModel
{
    private const int DefaultPipeStackSize = 10;
    private const double DefaultTileRenderSize = 120d;
    private const double DefaultTileSpacing = 8d;
    private const double DefaultBoardPadding = 48d;
    private static readonly string[] TileBackgroundImages =
    [
        "empty_tile_background_01.png",
        "empty_tile_background_02.png",
        "empty_tile_background_03.png",
        "empty_tile_background_04.png",
        "empty_tile_background_05.png",
        "empty_tile_background_06.png",
        "empty_tile_background_07.png",
        "empty_tile_background_08.png",
        "empty_tile_background_09.png",
        "empty_tile_background_10.png",
        "empty_tile_background_11.png",
        "empty_tile_background_12.png",
        "empty_tile_background_13.png",
        "empty_tile_background_14.png",
        "empty_tile_background_15.png",
        "empty_tile_background_16.png"
    ];

    private readonly INavigationService navigation;
    private readonly ILocalStateService localState;
    private readonly INetworkStatusService networkStatus;
    private readonly ILevelCacheService levelCacheService;
    private readonly ILevelsApiService levelsApiService;
    private readonly ILogger<GameplayViewModel> logger;

    private string levelId = string.Empty;
    private string loadedLevelId = string.Empty;
    private string displayName = string.Empty;
    private string difficulty = string.Empty;
    private string loadErrorMessage = string.Empty;
    private bool isPaused;
    private bool isGameOver;
    private bool isSuccess;
    private bool isLevelLoaded;
    private bool isPreStartModalVisible;
    private int score;
    private int boardWidth;
    private int boardHeight;
    private int flowTimeoutSeconds;
    private int flowSpeedIndicator;
    private int remainingPrepSeconds;
    private int pipeStackAnimationVersion;
    private Color prepTimerColor = Colors.Gold;
    private double prepTimerOpacity = 1d;
    private bool prepTimerBlinkPhaseVisible = true;
    private CancellationTokenSource? prepCountdownCancellationTokenSource;

    public ObservableCollection<PlayfieldTileItem> BoardTiles { get; } = [];
    public ObservableCollection<PipeStackItem> UpcomingPipes { get; } = [];

    public string LevelId
    {
        get => levelId;
        set
        {
            if (SetField(ref levelId, value))
            {
                loadedLevelId = string.Empty;
            }
        }
    }

    public string DisplayName
    {
        get => displayName;
        private set
        {
            if (SetField(ref displayName, value))
            {
                OnPropertyChanged(nameof(LevelDisplayName));
                OnPropertyChanged(nameof(LevelNumberLabel));
            }
        }
    }

    public string Difficulty
    {
        get => difficulty;
        private set => SetField(ref difficulty, value);
    }

    public string LevelDisplayName => string.IsNullOrWhiteSpace(DisplayName) ? LevelId : DisplayName;

    public string LevelNumberLabel => ExtractLevelNumber(LevelId, DisplayName);

    public string LoadErrorMessage
    {
        get => loadErrorMessage;
        private set
        {
            if (SetField(ref loadErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasLoadError));
            }
        }
    }

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    public bool HasLevelLoaded
    {
        get => isLevelLoaded;
        private set => SetField(ref isLevelLoaded, value);
    }

    public bool IsPreStartModalVisible
    {
        get => isPreStartModalVisible;
        set => SetField(ref isPreStartModalVisible, value);
    }

    public bool IsPaused
    {
        get => isPaused;
        set => SetField(ref isPaused, value);
    }

    public bool IsGameOver
    {
        get => isGameOver;
        set => SetField(ref isGameOver, value);
    }

    public bool IsSuccess
    {
        get => isSuccess;
        set => SetField(ref isSuccess, value);
    }

    public int Score
    {
        get => score;
        set => SetField(ref score, value);
    }

    public int BoardWidth
    {
        get => boardWidth;
        private set
        {
            if (SetField(ref boardWidth, value))
            {
                OnPropertyChanged(nameof(BoardPixelWidth));
            }
        }
    }

    public int BoardHeight
    {
        get => boardHeight;
        private set
        {
            if (SetField(ref boardHeight, value))
            {
                OnPropertyChanged(nameof(BoardPixelHeight));
            }
        }
    }

    public int FlowTimeoutSeconds
    {
        get => flowTimeoutSeconds;
        private set
        {
            if (SetField(ref flowTimeoutSeconds, value))
            {
                OnPropertyChanged(nameof(FlowTimeoutLabel));
            }
        }
    }

    public int FlowSpeedIndicator
    {
        get => flowSpeedIndicator;
        private set => SetField(ref flowSpeedIndicator, value);
    }

    public string FlowTimeoutLabel => $"{FlowTimeoutSeconds} seconds";

    public int RemainingPrepSeconds
    {
        get => remainingPrepSeconds;
        set
        {
            if (SetField(ref remainingPrepSeconds, value))
            {
                UpdatePreparationCountdownPresentation();
            }
        }
    }

    public Color PrepTimerColor
    {
        get => prepTimerColor;
        private set => SetField(ref prepTimerColor, value);
    }

    public double PrepTimerOpacity
    {
        get => prepTimerOpacity;
        private set => SetField(ref prepTimerOpacity, value);
    }

    public int PipeStackAnimationVersion
    {
        get => pipeStackAnimationVersion;
        private set => SetField(ref pipeStackAnimationVersion, value);
    }

    public double TileRenderSize => DefaultTileRenderSize;

    public double TileSpacing => DefaultTileSpacing;

    public double MaxTileZoom => PlayfieldViewportMath.CalculateMaxZoomForTileSize(TileRenderSize);

    public double BoardPixelWidth =>
        BoardWidth <= 0
            ? 0
            : (BoardWidth * TileRenderSize) + (Math.Max(0, BoardWidth - 1) * TileSpacing) + DefaultBoardPadding;

    public double BoardPixelHeight =>
        BoardHeight <= 0
            ? 0
            : (BoardHeight * TileRenderSize) + (Math.Max(0, BoardHeight - 1) * TileSpacing) + DefaultBoardPadding;

    public Command PauseCommand { get; }
    public Command ResumeCommand { get; }
    public Command QuitCommand { get; }
    public Command RetryCommand { get; }
    public Command StartLevelCommand { get; }

    public GameplayViewModel(
        INavigationService navigation,
        ILocalStateService localState,
        INetworkStatusService networkStatus,
        ILevelCacheService levelCacheService,
        ILevelsApiService levelsApiService,
        ILogger<GameplayViewModel> logger)
    {
        this.navigation = navigation;
        this.localState = localState;
        this.networkStatus = networkStatus;
        this.levelCacheService = levelCacheService;
        this.levelsApiService = levelsApiService;
        this.logger = logger;

        PauseCommand = new Command(() =>
        {
            if (HasLevelLoaded && !IsPreStartModalVisible)
            {
                IsPaused = true;
                RecordUserAction("pause");
                logger.LogInformation("Paused gameplay for level {LevelId}.", LevelId);
            }
        });

        ResumeCommand = new Command(() =>
        {
            IsPaused = false;
            RecordUserAction("resume");
            logger.LogInformation("Resumed gameplay for level {LevelId}.", LevelId);
        });

        QuitCommand = new Command(async () =>
        {
            CancelPreparationCountdown();
            IsPaused = false;
            RecordUserAction("quit");
            logger.LogInformation("Leaving gameplay for level {LevelId}.", LevelId);
            await navigation.GoBackAsync();
        });

        RetryCommand = new Command(() =>
        {
            CancelPreparationCountdown();
            IsGameOver = false;
            IsSuccess = false;
            Score = 0;
            RemainingPrepSeconds = FlowTimeoutSeconds;
            IsPreStartModalVisible = HasLevelLoaded;
            RecordUserAction("retry");
            logger.LogInformation("Restarting the pre-start flow for level {LevelId}.", LevelId);
        });

        StartLevelCommand = new Command(StartLevel);
        UpdatePreparationCountdownPresentation();
    }

    public async Task LoadLevelAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(LevelId))
        {
            return;
        }

        if (HasLevelLoaded && string.Equals(LevelId, loadedLevelId, StringComparison.Ordinal))
        {
            logger.LogDebug("Skipping level load because {LevelId} is already loaded.", LevelId);
            return;
        }

        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("gameplay.load-level", ActivityKind.Internal);
        activity?.SetTag("level.id", LevelId);
        activity?.SetTag("network.online", networkStatus.HasInternetAccess);

        var stopwatch = Stopwatch.StartNew();
        CancelPreparationCountdown();
        IsBusy = true;
        LoadErrorMessage = string.Empty;
        IsPaused = false;
        IsPreStartModalVisible = false;
        HasLevelLoaded = false;
        BoardTiles.Clear();
        UpcomingPipes.Clear();
        BoardWidth = 0;
        BoardHeight = 0;
        logger.LogInformation("Loading gameplay level {LevelId}. Online={HasInternetAccess}.", LevelId, networkStatus.HasInternetAccess);

        try
        {
            var (releasedLevel, levelRevision, source) = await ResolveLevelAsync(cancellationToken);

            ApplyLevel(releasedLevel, levelRevision);

            localState.SetCurrentLevelId(releasedLevel.LevelId);
            loadedLevelId = releasedLevel.LevelId;
            HasLevelLoaded = true;

            activity?.SetTag("level.source", source);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Loaded level {LevelId} from {Source}.", releasedLevel.LevelId, source);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Canceled level load for {LevelId}.", LevelId);
        }
        catch (HttpRequestException exception)
        {
            LoadErrorMessage = "Couldn't reach the FloodRush API to load this level.";
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Loading level {LevelId} failed because the FloodRush API could not be reached.", LevelId);
        }
        catch (InvalidOperationException exception)
        {
            LoadErrorMessage = exception.Message;
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Loading level {LevelId} failed because the level data was unavailable or invalid.", LevelId);
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "gameplay-level-load" }
            });
            IsBusy = false;
        }
    }

    private async Task<(ReleasedLevelSummaryDto ReleasedLevel, LevelRevisionDto Revision, string Source)> ResolveLevelAsync(
        CancellationToken cancellationToken)
    {
        if (networkStatus.HasInternetAccess)
        {
            try
            {
                var releasedLevels = await levelsApiService.GetReleasedLevelsAsync(cancellationToken);
                var releasedLevel = releasedLevels.FirstOrDefault(level =>
                    string.Equals(level.LevelId, LevelId, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException($"The server did not return released level '{LevelId}'.");

                var levelRevision = await levelsApiService.GetLevelRevisionAsync(
                    releasedLevel.LevelId,
                    releasedLevel.Revision,
                    cancellationToken);

                await levelCacheService.SaveLevelRevisionAsync(levelRevision, cancellationToken);
                await levelCacheService.SaveReleasedLevelsAsync(releasedLevels, cancellationToken);

                return (releasedLevel, levelRevision, "server");
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Falling back to the cached level revision for {LevelId} after a server request failed.", LevelId);
            }
        }

        var cachedReleasedLevels = await levelCacheService.GetReleasedLevelsAsync(cancellationToken);
        var cachedReleasedLevel = cachedReleasedLevels.FirstOrDefault(level =>
            string.Equals(level.LevelId, LevelId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Level '{LevelId}' is not available in the local cache.");

        var cachedRevision = await levelCacheService.GetLevelRevisionAsync(
            cachedReleasedLevel.LevelId,
            cachedReleasedLevel.Revision,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Level '{cachedReleasedLevel.LevelId}' is listed locally but its cached revision is missing.");

        return (cachedReleasedLevel, cachedRevision, "cache");
    }

    private void ApplyLevel(ReleasedLevelSummaryDto releasedLevel, LevelRevisionDto levelRevision)
    {
        DisplayName = levelRevision.DisplayName;
        Difficulty = string.IsNullOrWhiteSpace(levelRevision.Difficulty)
            ? releasedLevel.Difficulty
            : levelRevision.Difficulty;
        FlowSpeedIndicator = levelRevision.FlowSpeedIndicator;
        FlowTimeoutSeconds = (int)Math.Ceiling(TimeSpan.FromMilliseconds(levelRevision.StartDelayMilliseconds).TotalSeconds);
        RemainingPrepSeconds = FlowTimeoutSeconds;
        Score = 0;
        IsGameOver = false;
        IsSuccess = false;
        BoardWidth = levelRevision.BoardWidth;
        BoardHeight = levelRevision.BoardHeight;

        BoardTiles.Clear();
        foreach (var tile in BuildTiles(levelRevision))
        {
            BoardTiles.Add(tile);
        }

        foreach (var pipe in BuildPipeStack())
        {
            UpcomingPipes.Add(pipe);
        }

        PipeStackAnimationVersion++;
    }

    private void StartLevel()
    {
        if (!HasLevelLoaded)
        {
            return;
        }

        IsPreStartModalVisible = false;
        RemainingPrepSeconds = FlowTimeoutSeconds;
        RecordUserAction("start-level");
        logger.LogInformation("Starting level countdown for {LevelId}.", LevelId);
        StartPreparationCountdown();
    }

    private void StartPreparationCountdown()
    {
        CancelPreparationCountdown();
        if (RemainingPrepSeconds <= 0)
        {
            return;
        }

        prepTimerBlinkPhaseVisible = true;
        UpdatePreparationCountdownPresentation();
        prepCountdownCancellationTokenSource = new CancellationTokenSource();
        _ = RunPreparationCountdownAsync(prepCountdownCancellationTokenSource.Token);
    }

    private async Task RunPreparationCountdownAsync(CancellationToken cancellationToken)
    {
        var secondTickStopwatch = Stopwatch.StartNew();
        var blinkStopwatch = Stopwatch.StartNew();

        try
        {
            while (RemainingPrepSeconds > 0)
            {
                while (IsPaused || IsPreStartModalVisible)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                    secondTickStopwatch.Restart();
                    blinkStopwatch.Restart();
                    prepTimerBlinkPhaseVisible = true;
                    UpdatePreparationCountdownPresentation();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

                if (PreparationCountdownPresentation.ShouldBlink(RemainingPrepSeconds))
                {
                    var isBlinkPhaseVisible = (blinkStopwatch.ElapsedMilliseconds / 500) % 2 == 0;
                    if (prepTimerBlinkPhaseVisible != isBlinkPhaseVisible)
                    {
                        prepTimerBlinkPhaseVisible = isBlinkPhaseVisible;
                        UpdatePreparationCountdownPresentation();
                    }
                }
                else if (!prepTimerBlinkPhaseVisible)
                {
                    prepTimerBlinkPhaseVisible = true;
                    UpdatePreparationCountdownPresentation();
                }

                if (!IsPaused &&
                    !IsPreStartModalVisible &&
                    RemainingPrepSeconds > 0 &&
                    secondTickStopwatch.Elapsed >= TimeSpan.FromSeconds(1))
                {
                    RemainingPrepSeconds--;
                    secondTickStopwatch.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelPreparationCountdown()
    {
        prepCountdownCancellationTokenSource?.Cancel();
        prepCountdownCancellationTokenSource?.Dispose();
        prepCountdownCancellationTokenSource = null;
        prepTimerBlinkPhaseVisible = true;
        UpdatePreparationCountdownPresentation();
    }

    private void UpdatePreparationCountdownPresentation()
    {
        var urgency = PreparationCountdownPresentation.ResolveUrgency(RemainingPrepSeconds);

        PrepTimerColor = urgency switch
        {
            PreparationCountdownUrgency.Normal => Colors.Gold,
            PreparationCountdownUrgency.Warning => Colors.Orange,
            PreparationCountdownUrgency.Critical => Colors.Red,
            _ => Colors.Gold
        };

        PrepTimerOpacity = PreparationCountdownPresentation.ResolveOpacity(
            RemainingPrepSeconds,
            prepTimerBlinkPhaseVisible);
    }

    private void RecordUserAction(string action)
    {
        FloodRushTelemetry.UserActions.Add(1, new TagList
        {
            { "screen", "gameplay" },
            { "action", action }
        });
    }

    private static IReadOnlyCollection<PlayfieldTileItem> BuildTiles(LevelRevisionDto levelRevision)
    {
        var fixedTileLookup = levelRevision.FixedTiles.ToDictionary(tile => (tile.X, tile.Y));
        var tiles = new List<PlayfieldTileItem>(levelRevision.BoardWidth * levelRevision.BoardHeight);
        var random = new Random(HashCode.Combine(levelRevision.LevelId, levelRevision.Revision));

        for (var y = 0; y < levelRevision.BoardHeight; y++)
        {
            for (var x = 0; x < levelRevision.BoardWidth; x++)
            {
                var backgroundImage = TileBackgroundImages[random.Next(TileBackgroundImages.Length)];

                if (fixedTileLookup.TryGetValue((x, y), out var fixedTile))
                {
                    tiles.Add(new PlayfieldTileItem(
                        x,
                        y,
                        MapTileKind(fixedTile.TileType),
                        backgroundImage,
                        GetTileTitle(fixedTile.TileType),
                        GetTileSubtitle(fixedTile)));
                }
                else
                {
                    tiles.Add(new PlayfieldTileItem(x, y, PlayfieldTileKind.Empty, backgroundImage, string.Empty, string.Empty));
                }
            }
        }

        return tiles;
    }

    private IReadOnlyCollection<PipeStackItem> BuildPipeStack()
    {
        var generatedPipeTypes = PipeStackGenerator.GenerateInitialStack(
            DefaultPipeStackSize,
            Random.Shared.Next());

        logger.LogInformation("Generated a gameplay pipe stack with {Count} upcoming tiles.", generatedPipeTypes.Count);

        return generatedPipeTypes
            .Select((pipeType, index) =>
            {
                var isNextToPlace = index == generatedPipeTypes.Count - 1;
                return new PipeStackItem(
                    pipeType,
                    GetPipeGlyph(pipeType),
                    GetPipeLabel(pipeType),
                    isNextToPlace ? "Next to place" : "Queued above the board",
                    isNextToPlace,
                    isNextToPlace ? 1d : 0.72d);
            })
            .ToArray();
    }

    private static string GetPipeGlyph(PipeSectionType pipeType) =>
        pipeType switch
        {
            PipeSectionType.Horizontal => "━",
            PipeSectionType.Vertical => "┃",
            PipeSectionType.CornerLeftToTop => "┛",
            PipeSectionType.CornerRightToTop => "┗",
            PipeSectionType.CornerLeftToBottom => "┓",
            PipeSectionType.CornerRightToBottom => "┏",
            PipeSectionType.Cross => "╋",
            _ => "?"
        };

    private static string GetPipeLabel(PipeSectionType pipeType) =>
        pipeType switch
        {
            PipeSectionType.Horizontal => "Horizontal",
            PipeSectionType.Vertical => "Vertical",
            PipeSectionType.CornerLeftToTop => "Left to top",
            PipeSectionType.CornerRightToTop => "Right to top",
            PipeSectionType.CornerLeftToBottom => "Left to bottom",
            PipeSectionType.CornerRightToBottom => "Right to bottom",
            PipeSectionType.Cross => "Cross",
            _ => pipeType.ToString()
        };

    private static PlayfieldTileKind MapTileKind(LevelFixedTileTypeDto tileType) =>
        tileType switch
        {
            LevelFixedTileTypeDto.StartPoint => PlayfieldTileKind.StartPoint,
            LevelFixedTileTypeDto.FinishPoint => PlayfieldTileKind.FinishPoint,
            LevelFixedTileTypeDto.FluidBasin => PlayfieldTileKind.FluidBasin,
            LevelFixedTileTypeDto.SplitSection => PlayfieldTileKind.SplitSection,
            _ => PlayfieldTileKind.Empty
        };

    private static string GetTileTitle(LevelFixedTileTypeDto tileType) =>
        tileType switch
        {
            LevelFixedTileTypeDto.StartPoint => "Start",
            LevelFixedTileTypeDto.FinishPoint => "Finish",
            LevelFixedTileTypeDto.FluidBasin => "Basin",
            LevelFixedTileTypeDto.SplitSection => "Split",
            _ => string.Empty
        };

    private static string GetTileSubtitle(LevelFixedTileDto tile) =>
        tile.TileType switch
        {
            LevelFixedTileTypeDto.StartPoint => $"Exit {MapDirection(tile.OutputDirection)}",
            LevelFixedTileTypeDto.FinishPoint => $"Enter {MapDirection(tile.EntryDirection)}",
            LevelFixedTileTypeDto.FluidBasin => tile.FillDelayMilliseconds.HasValue
                ? $"{Math.Ceiling(TimeSpan.FromMilliseconds(tile.FillDelayMilliseconds.Value).TotalSeconds):0}s fill"
                : "Delay tile",
            LevelFixedTileTypeDto.SplitSection => tile.SpeedModifierPercent.HasValue
                ? $"{tile.SpeedModifierPercent.Value}% speed"
                : "Branch flow",
            _ => string.Empty
        };

    private static string MapDirection(BoardDirectionDto? direction) =>
        direction switch
        {
            BoardDirectionDto.Left => "←",
            BoardDirectionDto.Top => "↑",
            BoardDirectionDto.Right => "→",
            BoardDirectionDto.Bottom => "↓",
            _ => string.Empty
        };

    private static string ExtractLevelNumber(string levelId, string displayName)
    {
        var source = !string.IsNullOrWhiteSpace(displayName) ? displayName : levelId;
        var digits = new string(source.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? "Level" : $"Level {int.Parse(digits)}";
    }
}
