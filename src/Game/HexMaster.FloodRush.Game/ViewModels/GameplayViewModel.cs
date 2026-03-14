using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Controls;
using HexMaster.FloodRush.Game.Core.Domain.Board;
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
    private const double DefaultTileRenderSize = 64d;
    private const double DefaultTileSpacing = 0d;
    private const double DefaultBoardPadding = 16d;
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
    private readonly IScoresApiService scoresApiService;
    private readonly ILogger<GameplayViewModel> logger;

    private string levelId = string.Empty;
    private string loadedLevelId = string.Empty;
    private string displayName = string.Empty;
    private string difficulty = string.Empty;
    private string loadErrorMessage = string.Empty;
    private bool isPaused;
    private bool isFastForward;
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
    private bool isFlowActive;
    private bool isReplacementPenaltyActive;
    private bool isScoreSubmitting;
    private bool isRetrying;
    private int? playerHighScore;
    private int? globalHighScore;
    private LevelRevisionDto? loadedRevision;
    private ReleasedLevelSummaryDto? loadedReleasedLevel;
    private readonly HashSet<(int X, int Y)> visitedTiles = [];
    private readonly HashSet<(int X, int Y)> mandatoryBasinPositions = [];
    private readonly HashSet<(int X, int Y)> traversedBasinPositions = [];

    /// <summary>
    /// Fired when the flow engine wants to animate fluid on a specific tile.
    /// Handlers must marshal the animation call to the UI thread.
    /// </summary>
    public event EventHandler<BeginTileFlowEventArgs>? BeginTileFlow;

    /// <summary>
    /// Fired when fast-forward is toggled on while flow is active. Handlers should
    /// cancel any in-progress tile animation so the next tile starts at fast speed.
    /// </summary>
    public event EventHandler? CancelCurrentTileFlow;

    /// <summary>
    /// Fired when the player taps an occupied tile and the 3-second removal
    /// penalty animation must play before the new pipe is committed.
    /// The handler is responsible for running the animation and calling
    /// <see cref="PipeRemovalEventArgs.Complete"/> when finished.
    /// </summary>
    public event EventHandler<PipeRemovalEventArgs>? PipeRemovalStarted;

    public BatchObservableCollection<PlayfieldTileItem> BoardTiles { get; } = [];
    public BatchObservableCollection<PipeStackItem> UpcomingPipes { get; } = [];

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

    public bool IsFastForward
    {
        get => isFastForward;
        set
        {
            if (SetField(ref isFastForward, value))
            {
                OnPropertyChanged(nameof(FastForwardButtonText));
                if (isFastForward && isFlowActive)
                    CancelCurrentTileFlow?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string FastForwardButtonText => IsFastForward ? "Normal" : "Fast Fwd";

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

    /// <summary>
    /// True while the completed-level score is being posted to the server.
    /// Bound to a small indicator in the level-complete overlay.
    /// </summary>
    public bool IsScoreSubmitting
    {
        get => isScoreSubmitting;
        private set => SetField(ref isScoreSubmitting, value);
    }

    /// <summary>
    /// True from the moment the player taps "Try Again" until the reset
    /// animation finishes and the pre-start modal appears. Drives a
    /// loading overlay so the player knows the reset is in progress.
    /// </summary>
    public bool IsRetrying
    {
        get => isRetrying;
        set => SetField(ref isRetrying, value);
    }

    public int? PlayerHighScore
    {
        get => playerHighScore;
        private set => SetField(ref playerHighScore, value);
    }

    public int? GlobalHighScore
    {
        get => globalHighScore;
        private set => SetField(ref globalHighScore, value);
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

    public double MinTileZoom => PlayfieldViewportMath.DefaultMinZoom;

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
    public Command FastForwardCommand { get; }
    public Command QuitCommand { get; }
    public Command RetryCommand { get; }
    public Command NextLevelCommand { get; }
    public Command StartLevelCommand { get; }

    public GameplayViewModel(
        INavigationService navigation,
        ILocalStateService localState,
        INetworkStatusService networkStatus,
        ILevelCacheService levelCacheService,
        ILevelsApiService levelsApiService,
        IScoresApiService scoresApiService,
        ILogger<GameplayViewModel> logger)
    {
        this.navigation = navigation;
        this.localState = localState;
        this.networkStatus = networkStatus;
        this.levelCacheService = levelCacheService;
        this.levelsApiService = levelsApiService;
        this.scoresApiService = scoresApiService;
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

        FastForwardCommand = new Command(() =>
        {
            if (HasLevelLoaded)
            {
                IsFastForward = !IsFastForward;
                RecordUserAction(IsFastForward ? "fast-forward-on" : "fast-forward-off");
                logger.LogInformation(
                    "Fast forward {State} for level {LevelId}.",
                    IsFastForward ? "enabled" : "disabled", LevelId);
            }
        });

        QuitCommand = new Command(async () =>
        {
            CancelPreparationCountdown();
            IsPaused = false;
            RecordUserAction("quit");
            logger.LogInformation("Leaving gameplay for level {LevelId}.", LevelId);
            await navigation.GoBackAsync();
        });

        RetryCommand = new Command(async () =>
        {
            if (loadedReleasedLevel is null || loadedRevision is null)
            {
                return;
            }

            CancelPreparationCountdown();
            IsGameOver = false;
            IsFastForward = false;
            IsRetrying = true;
            RecordUserAction("retry");
            logger.LogInformation("Retrying level {LevelId}.", LevelId);

            // Build tile data on a background thread so the "Resetting level..." overlay
            // stays visible and responsive. The final ApplyLevel call (which fires one
            // Reset notification) happens back on the main thread.
            var captured = (loadedReleasedLevel, loadedRevision);
            var prebuiltTiles = await Task.Run(() => BuildTiles(captured.loadedRevision));
            ApplyLevel(captured.loadedReleasedLevel, captured.loadedRevision, prebuiltTiles);
        });

        NextLevelCommand = new Command(async () =>
        {
            CancelPreparationCountdown();
            IsSuccess = false;
            RecordUserAction("next-level");
            logger.LogInformation("Navigating to level selection after completing level {LevelId}.", LevelId);
            // Navigate to level selection so the player can pick the next level.
            // The list is refreshed so newly released levels appear immediately.
            await navigation.NavigateToLevelSelectionAsync(refreshLevels: true);
        });

        StartLevelCommand = new Command(StartLevel);
        UpdatePreparationCountdownPresentation();
    }

    public async Task LoadLevelAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("LoadLevelAsync called. LevelId='{LevelId}', IsBusy={IsBusy}.", LevelId, IsBusy);

        if (IsBusy || string.IsNullOrWhiteSpace(LevelId))
        {
            logger.LogWarning("LoadLevelAsync skipped: LevelId is '{LevelId}', IsBusy={IsBusy}.", LevelId, IsBusy);
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

            // Build the tile list on a background thread — it is pure in-memory work with no UI
            // dependencies. ApplyLevel then fires one Reset notification on the calling thread
            // (which is the main thread via MAUI's synchronization context after the await).
            var prebuiltTiles = await Task.Run(() => BuildTiles(levelRevision), cancellationToken);
            ApplyLevel(releasedLevel, levelRevision, prebuiltTiles);

            localState.SetCurrentLevelId(releasedLevel.LevelId);
            loadedLevelId = releasedLevel.LevelId;
            HasLevelLoaded = true;

            // Fire-and-forget: load highscores in the background while the player prepares.
            _ = LoadHighScoresInBackgroundAsync(releasedLevel.LevelId);

            activity?.SetTag("level.source", source);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Loaded level {LevelId} from {Source}.", releasedLevel.LevelId, source);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Level load for {LevelId} was canceled by the user.", LevelId);
        }
        catch (OperationCanceledException exception)
        {
            // Not a user cancellation — most likely an HTTP client timeout.
            LoadErrorMessage = "Level load timed out. Check your connection and try again.";
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Level load for {LevelId} timed out (likely an HTTP client timeout).", LevelId);
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
        catch (Exception exception)
        {
            LoadErrorMessage = "An unexpected error occurred while loading this level.";
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogError(exception, "Unexpected error loading level {LevelId}.", LevelId);
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
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                // Fall back to cache whether the server was unreachable (HttpRequestException)
                // or returned a valid response that did not include this level (InvalidOperationException).
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
        => ApplyLevel(releasedLevel, levelRevision, BuildTiles(levelRevision));

    private void ApplyLevel(
        ReleasedLevelSummaryDto releasedLevel,
        LevelRevisionDto levelRevision,
        IReadOnlyCollection<PlayfieldTileItem> prebuiltTiles)
    {
        loadedReleasedLevel = releasedLevel;
        loadedRevision = levelRevision;
        isFlowActive = false;
        isReplacementPenaltyActive = false;
        visitedTiles.Clear();
        traversedBasinPositions.Clear();
        mandatoryBasinPositions.Clear();
        mandatoryBasinPositions.UnionWith(
            (levelRevision.FixedTiles ?? [])
                .Where(t => t.TileType == LevelFixedTileTypeDto.FluidBasin && t.IsMandatory)
                .Select(t => (t.X, t.Y)));
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
        PlayerHighScore = null;
        GlobalHighScore = null;
        BoardWidth = levelRevision.BoardWidth;
        BoardHeight = levelRevision.BoardHeight;

        // ResetTo fires exactly ONE CollectionChanged(Reset) instead of one Add per tile.
        // Without this, PlayfieldBoardView.Rebuild() was called N times for N tiles,
        // creating an O(N²) rebuild cascade that could take 15+ minutes on large boards.
        BoardTiles.ResetTo(prebuiltTiles);
        UpcomingPipes.ResetTo(BuildPipeStack());

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

        // Countdown finished naturally – start the fluid flow
        if (!cancellationToken.IsCancellationRequested)
        {
            BeginFlow();
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
        var fixedTileLookup = (levelRevision.FixedTiles ?? []).ToDictionary(tile => (tile.X, tile.Y));
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
                        GetTileSubtitle(fixedTile),
                        GetTileOverlayImage(fixedTile),
                        GetTileImageRotation(fixedTile)));
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
                    GetPipeImage(pipeType),
                    GetPipeLabel(pipeType),
                    isNextToPlace ? "Next to place" : "Queued above the board",
                    isNextToPlace,
                    isNextToPlace ? 1d : 0.72d);
            })
            .ToArray();
    }

    private static string GetPipeImage(PipeSectionType pipeType) =>
        pipeType switch
        {
            PipeSectionType.Horizontal => "pipe_section_horiontal.png",
            PipeSectionType.Vertical => "pipe_section_vertical.png",
            PipeSectionType.CornerLeftToTop => "pipe_section_corner_left_top.png",
            PipeSectionType.CornerRightToTop => "pipe_section_corner_right_top.png",
            PipeSectionType.CornerLeftToBottom => "pipe_section_corner_left_bottom.png",
            PipeSectionType.CornerRightToBottom => "pipe_section_corner_right_bottom.png",
            PipeSectionType.Cross => "pipe_section_cross.png",
            _ => string.Empty
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

    private static string GetTileOverlayImage(LevelFixedTileDto tile) =>
        tile.TileType switch
        {
            LevelFixedTileTypeDto.StartPoint => "pipe_section_start.png",
            LevelFixedTileTypeDto.FinishPoint => "pipe_section_end.png",
            LevelFixedTileTypeDto.FluidBasin => tile.IsMandatory
                ? "pipe_section_bassin_mandatory.png"
                : "pipe_section_bassin.png",
            _ => string.Empty
        };

    // Start image exits RIGHT by default; end image connects on LEFT by default.
    // Rotation is clockwise degrees to align the open end with the tile's actual direction.
    private static double GetTileImageRotation(LevelFixedTileDto tile) =>
        tile.TileType switch
        {
            LevelFixedTileTypeDto.StartPoint => tile.OutputDirection switch
            {
                BoardDirectionDto.Right => 0d,
                BoardDirectionDto.Bottom => 90d,
                BoardDirectionDto.Left => 180d,
                BoardDirectionDto.Top => 270d,
                _ => 0d
            },
            LevelFixedTileTypeDto.FinishPoint => tile.EntryDirection switch
            {
                BoardDirectionDto.Left => 0d,
                BoardDirectionDto.Top => 90d,
                BoardDirectionDto.Right => 180d,
                BoardDirectionDto.Bottom => 270d,
                _ => 0d
            },
            _ => 0d
        };

    private static string ExtractLevelNumber(string levelId, string displayName)
    {
        var source = !string.IsNullOrWhiteSpace(displayName) ? displayName : levelId;
        var digits = new string(source.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? "Level" : $"Level {int.Parse(digits)}";
    }

    // ── Pipe placement ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when fluid has already flowed through the tile at
    /// (<paramref name="x"/>, <paramref name="y"/>), making any new placement there illegal.
    /// </summary>
    public bool IsVisitedTile(int x, int y) => visitedTiles.Contains((x, y));

    /// <summary>
    /// Places the next-to-place pipe from the bottom of the stack onto the tile at
    /// (<paramref name="x"/>, <paramref name="y"/>).
    /// Returns <c>true</c> when the tap was accepted.
    /// <paramref name="penaltyAnimationStarted"/> is set to <c>true</c> when the
    /// tile already had a pipe: in that case the caller must NOT animate the pipe
    /// stack immediately — the <see cref="PipeRemovalStarted"/> event's completion
    /// callback will trigger the stack animation after the 3-second penalty.
    /// </summary>
    public bool TryPlacePipe(int x, int y, out bool penaltyAnimationStarted)
    {
        penaltyAnimationStarted = false;

        if (!HasLevelLoaded || IsGameOver || IsSuccess ||
            IsPreStartModalVisible || IsPaused || isReplacementPenaltyActive)
        {
            return false;
        }

        var tile = BoardTiles.FirstOrDefault(t => t.X == x && t.Y == y);
        if (tile is null || tile.Kind != PlayfieldTileKind.Empty)
        {
            return false;
        }

        // A pipe that has already been traversed by fluid cannot be replaced.
        if (tile.PlacedPipeType is not null && visitedTiles.Contains((x, y)))
        {
            logger.LogDebug(
                "Cannot replace pipe at ({X},{Y}) – fluid has already flowed through it.", x, y);
            return false;
        }

        if (UpcomingPipes.Count == 0) return false;

        var tileIndex = -1;
        for (var i = 0; i < BoardTiles.Count; i++)
        {
            if (BoardTiles[i].X == x && BoardTiles[i].Y == y) { tileIndex = i; break; }
        }
        if (tileIndex < 0) return false;

        var nextPipe = UpcomingPipes[UpcomingPipes.Count - 1];

        if (tile.PlacedPipeType is not null)
        {
            // Occupied tile → 3-second penalty: block the board and fire the event.
            // The Page handler runs the animation, then calls Complete() which commits
            // the placement and lets the caller animate the pipe stack.
            isReplacementPenaltyActive = true;
            penaltyAnimationStarted = true;

            PipeRemovalStarted?.Invoke(this, new PipeRemovalEventArgs(x, y, () =>
            {
                isReplacementPenaltyActive = false;
                ExecutePlacement(tile, tileIndex, nextPipe);
                logger.LogInformation(
                    "Replaced {PipeType} pipe at ({X},{Y}) for level {LevelId}.",
                    nextPipe.PipeType, x, y, LevelId);
            }));

            return true;
        }

        // Empty tile → immediate placement.
        ExecutePlacement(tile, tileIndex, nextPipe);
        logger.LogInformation(
            "Placed {PipeType} pipe at ({X},{Y}) for level {LevelId}.",
            nextPipe.PipeType, x, y, LevelId);

        return true;
    }

    /// <summary>
    /// Commits the pipe placement: updates <see cref="BoardTiles"/>, removes the
    /// consumed pipe from the stack, promotes the new bottom item, and adds a
    /// fresh random pipe to the top.
    /// </summary>
    private void ExecutePlacement(PlayfieldTileItem tile, int tileIndex, PipeStackItem nextPipe)
    {
        BoardTiles[tileIndex] = tile with
        {
            PlacedPipeType = nextPipe.PipeType,
            PipeOverlayImage = GetPipeImage(nextPipe.PipeType),
            PipeImageRotation = 0d
        };

        UpcomingPipes.RemoveAt(UpcomingPipes.Count - 1);

        if (UpcomingPipes.Count > 0)
        {
            var lastIdx = UpcomingPipes.Count - 1;
            var prev = UpcomingPipes[lastIdx];
            UpcomingPipes[lastIdx] = prev with
            {
                IsNextToPlace = true,
                QueueLabel = "Next to place",
                PreviewOpacity = 1d
            };
        }

        var newType = PipeStackGenerator.GenerateInitialStack(1, Random.Shared.Next()).First();
        UpcomingPipes.Insert(0, new PipeStackItem(
            newType,
            GetPipeImage(newType),
            GetPipeLabel(newType),
            "Queued above the board",
            false,
            0.72d));
    }

    // ── Flow orchestration ──────────────────────────────────────────────────────

    private void BeginFlow()
    {
        if (!HasLevelLoaded || loadedRevision is null) return;

        var startFixed = loadedRevision.FixedTiles
            .FirstOrDefault(t => t.TileType == LevelFixedTileTypeDto.StartPoint);
        if (startFixed is null)
        {
            EndFlowFailed();
            return;
        }

        isFlowActive = true;
        var exitDir = ToGameDirection(startFixed.OutputDirection ?? BoardDirectionDto.Right);
        var entryDir = Opposite(exitDir);

        logger.LogInformation(
            "Flow starting at ({X},{Y}) exiting {Exit} for level {LevelId}.",
            startFixed.X, startFixed.Y, exitDir, LevelId);

        BeginTileFlow?.Invoke(this, new BeginTileFlowEventArgs
        {
            X = startFixed.X,
            Y = startFixed.Y,
            EntryDirection = entryDir,
            ExitDirection = exitDir,
            Points = startFixed.BonusPoints,
            DurationMs = CalculateFlowDuration(),
            IsTerminal = false
        });
    }

    /// <summary>
    /// Called by <see cref="Pages.GameplayPage"/> when a tile finishes its flow animation.
    /// Updates the score and triggers the next tile's animation, or ends the flow.
    /// </summary>
    public void OnTileFlowCompleted(TileFlowCompletedEventArgs e)
    {
        visitedTiles.Add((e.X, e.Y));
        Score += e.PointsEarned;

        if (e.IsTerminal)
        {
            isFlowActive = false;
            if (!mandatoryBasinPositions.All(p => traversedBasinPositions.Contains(p)))
            {
                EndFlowFailed();
                return;
            }

            IsSuccess = true;
            localState.RecordLevelCompletion(LevelId);
            logger.LogInformation(
                "Level {LevelId} completed successfully. Final score: {Score}.", LevelId, Score);

            // Fire-and-forget: submit the score to the server in the background.
            // The overlay is shown immediately; IsScoreSubmitting drives a small indicator.
            _ = SubmitCompletedLevelScoreAsync();
            return;
        }

        if (!isFlowActive) return;

        var (nextX, nextY) = GetAdjacentPosition(e.X, e.Y, e.ExitDirection);
        var entryDir = Opposite(e.ExitDirection);

        var nextTile = BoardTiles.FirstOrDefault(t => t.X == nextX && t.Y == nextY);
        if (nextTile is null)
        {
            logger.LogInformation(
                "Flow exited the board at ({X},{Y}) → ({NX},{NY}) for level {LevelId}.",
                e.X, e.Y, nextX, nextY, LevelId);
            EndFlowFailed();
            return;
        }

        var flowInfo = GetTileFlowInfo(nextTile, entryDir);
        if (!flowInfo.HasValue)
        {
            logger.LogInformation(
                "Flow blocked at ({X},{Y}) entering {Entry} for level {LevelId}.",
                nextX, nextY, entryDir, LevelId);
            EndFlowFailed();
            return;
        }

        var (nextExitDir, nextPoints, isTerminal) = flowInfo.Value;

        BeginTileFlow?.Invoke(this, new BeginTileFlowEventArgs
        {
            X = nextX,
            Y = nextY,
            EntryDirection = entryDir,
            ExitDirection = nextExitDir,
            Points = nextPoints,
            DurationMs = nextTile.Kind == PlayfieldTileKind.FluidBasin
                ? CalculateFlowDuration() * 3
                : CalculateFlowDuration(),
            IsTerminal = isTerminal
        });
    }

    private (BoardDirection exitDir, int points, bool isTerminal)? GetTileFlowInfo(
        PlayfieldTileItem tile, BoardDirection entry)
    {
        var fixedTile = loadedRevision?.FixedTiles
            .FirstOrDefault(f => f.X == tile.X && f.Y == tile.Y);

        if (fixedTile is not null)
        {
            return fixedTile.TileType switch
            {
                LevelFixedTileTypeDto.FinishPoint => HandleFinishPoint(fixedTile, entry),
                LevelFixedTileTypeDto.FluidBasin => HandleBasinTile(fixedTile, entry),
                LevelFixedTileTypeDto.StartPoint => null, // cannot re-enter start
                _ => null
            };
        }

        if (tile.PlacedPipeType is null) return null; // empty tile – flow blocked

        try
        {
            var exitDir = GetPipeExitDirection(tile.PlacedPipeType.Value, entry);
            // A cross section that has already been traversed on the perpendicular axis
            // awards the second-pass bonus instead of the base points.
            var pts = tile.PlacedPipeType.Value == PipeSectionType.Cross
                      && visitedTiles.Contains((tile.X, tile.Y))
                ? 50
                : GetPipeBasePoints(tile.PlacedPipeType.Value);
            return (exitDir, pts, false);
        }
        catch (InvalidOperationException)
        {
            return null; // incompatible entry direction
        }
    }

    private static (BoardDirection exitDir, int points, bool isTerminal)? HandleFinishPoint(
        LevelFixedTileDto fixedTile, BoardDirection entry)
    {
        var expected = ToGameDirection(fixedTile.EntryDirection ?? BoardDirectionDto.Left);
        return expected == entry
            ? (entry, fixedTile.BonusPoints, true)
            : null; // fluid arrived from the wrong side
    }

    private (BoardDirection exitDir, int points, bool isTerminal)? HandleBasinTile(
        LevelFixedTileDto fixedTile, BoardDirection entry)
    {
        var expected = ToGameDirection(fixedTile.EntryDirection ?? BoardDirectionDto.Left);
        if (expected != entry) return null; // fluid arrived from wrong side
        traversedBasinPositions.Add((fixedTile.X, fixedTile.Y));
        var exitDir = ToGameDirection(fixedTile.OutputDirection ?? BoardDirectionDto.Right);
        return (exitDir, fixedTile.BonusPoints, false);
    }

    private async Task LoadHighScoresInBackgroundAsync(string levelId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = cts.Token;

        await Task.WhenAll(
            LoadPlayerHighScoreAsync(levelId, token),
            LoadGlobalHighScoreAsync(levelId, token));
    }

    private async Task LoadPlayerHighScoreAsync(string levelId, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await scoresApiService.GetPlayerBestScoreAsync(levelId, cancellationToken);
            PlayerHighScore = dto?.Points;
            logger.LogDebug("Player best score for level {LevelId}: {Points}.", levelId, PlayerHighScore);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not load player best score for level {LevelId}.", levelId);
        }
    }

    private async Task LoadGlobalHighScoreAsync(string levelId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await scoresApiService.GetTopScoresAsync(levelId, take: 1, cancellationToken);
            GlobalHighScore = response?.Scores.FirstOrDefault()?.Points;
            logger.LogDebug("Global best score for level {LevelId}: {Points}.", levelId, GlobalHighScore);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not load global best score for level {LevelId}.", levelId);
        }
    }

    private async Task SubmitCompletedLevelScoreAsync()
    {
        if (loadedRevision is null) return;

        IsScoreSubmitting = true;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var request = new HexMaster.FloodRush.Shared.Contracts.Scores.SubmitScoreRequest(
                LevelId,
                loadedRevision.Revision,
                Score,
                DateTimeOffset.UtcNow);

            await scoresApiService.SubmitScoreAsync(request, cts.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to submit score for level {LevelId}.", LevelId);
        }
        finally
        {
            IsScoreSubmitting = false;
        }
    }

    private void EndFlowFailed()
    {
        isFlowActive = false;
        IsGameOver = true;
        logger.LogInformation(
            "Flow failed for level {LevelId}. Score at failure: {Score}.", LevelId, Score);
    }

    private int CalculateFlowDuration() =>
        IsFastForward ? 500 : Math.Max(300, (101 - FlowSpeedIndicator) * 100);

    private static (int x, int y) GetAdjacentPosition(int x, int y, BoardDirection direction) =>
        direction switch
        {
            BoardDirection.Left => (x - 1, y),
            BoardDirection.Right => (x + 1, y),
            BoardDirection.Top => (x, y - 1),
            BoardDirection.Bottom => (x, y + 1),
            _ => (x, y)
        };

    private static BoardDirection Opposite(BoardDirection dir) =>
        dir switch
        {
            BoardDirection.Left => BoardDirection.Right,
            BoardDirection.Right => BoardDirection.Left,
            BoardDirection.Top => BoardDirection.Bottom,
            BoardDirection.Bottom => BoardDirection.Top,
            _ => dir
        };

    private static BoardDirection ToGameDirection(BoardDirectionDto dto) =>
        (BoardDirection)(int)dto;

    private static BoardDirection GetPipeExitDirection(PipeSectionType pipeType, BoardDirection entry) =>
        (pipeType, entry) switch
        {
            (PipeSectionType.Horizontal, BoardDirection.Left) => BoardDirection.Right,
            (PipeSectionType.Horizontal, BoardDirection.Right) => BoardDirection.Left,
            (PipeSectionType.Vertical, BoardDirection.Top) => BoardDirection.Bottom,
            (PipeSectionType.Vertical, BoardDirection.Bottom) => BoardDirection.Top,
            (PipeSectionType.CornerLeftToTop, BoardDirection.Left) => BoardDirection.Top,
            (PipeSectionType.CornerLeftToTop, BoardDirection.Top) => BoardDirection.Left,
            (PipeSectionType.CornerRightToTop, BoardDirection.Right) => BoardDirection.Top,
            (PipeSectionType.CornerRightToTop, BoardDirection.Top) => BoardDirection.Right,
            (PipeSectionType.CornerLeftToBottom, BoardDirection.Left) => BoardDirection.Bottom,
            (PipeSectionType.CornerLeftToBottom, BoardDirection.Bottom) => BoardDirection.Left,
            (PipeSectionType.CornerRightToBottom, BoardDirection.Right) => BoardDirection.Bottom,
            (PipeSectionType.CornerRightToBottom, BoardDirection.Bottom) => BoardDirection.Right,
            (PipeSectionType.Cross, _) => Opposite(entry), // cross passes through on same axis
            _ => throw new InvalidOperationException(
                $"Cannot flow from {entry} through {pipeType}.")
        };

    // Base point values match PlaceablePipeSectionDefinition.CreateRequiredSections()
    private static int GetPipeBasePoints(PipeSectionType pipeType) =>
        pipeType switch
        {
            PipeSectionType.Horizontal => 10,
            PipeSectionType.Vertical => 10,
            PipeSectionType.CornerLeftToTop => 12,
            PipeSectionType.CornerRightToTop => 12,
            PipeSectionType.CornerLeftToBottom => 12,
            PipeSectionType.CornerRightToBottom => 12,
            PipeSectionType.Cross => 10,
            _ => 0
        };
}
