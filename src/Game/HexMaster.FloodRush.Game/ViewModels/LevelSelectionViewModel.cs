using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Game.Services;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.ViewModels;

[QueryProperty(nameof(RefreshLevels), "refreshLevels")]
public sealed class LevelSelectionViewModel : BaseViewModel
{
    private readonly INavigationService navigation;
    private readonly ILocalStateService localState;
    private readonly INetworkStatusService networkStatus;
    private readonly ILevelCacheService levelCacheService;
    private readonly ILevelsApiService levelsApiService;
    private readonly ILogger<LevelSelectionViewModel> logger;
    private string loadErrorMessage = string.Empty;
    private bool shouldRefreshOnNextAppearance;

    public ObservableCollection<LevelListItem> Levels { get; } = [];

    public string RefreshLevels
    {
        set => shouldRefreshOnNextAppearance = bool.TryParse(value, out var refreshLevels) && refreshLevels;
    }

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

    public Command<LevelListItem> SelectLevelCommand { get; }
    public Command BackCommand { get; }

    public LevelSelectionViewModel(
        INavigationService navigation,
        ILocalStateService localState,
        INetworkStatusService networkStatus,
        ILevelCacheService levelCacheService,
        ILevelsApiService levelsApiService,
        ILogger<LevelSelectionViewModel> logger)
    {
        this.navigation = navigation;
        this.localState = localState;
        this.networkStatus = networkStatus;
        this.levelCacheService = levelCacheService;
        this.levelsApiService = levelsApiService;
        this.logger = logger;

        SelectLevelCommand = new Command<LevelListItem>(async item =>
        {
            if (item is null)
            {
                return;
            }

            RecordUserAction("select-level");
            localState.SetCurrentLevelId(item.LevelId);
            logger.LogInformation("Selected level {LevelId} from the level selection screen.", item.LevelId);
            await navigation.NavigateToGameplayAsync(item.LevelId);
        });

        BackCommand = new Command(async () =>
        {
            RecordUserAction("back");
            await navigation.GoBackAsync();
        });
    }

    public async Task LoadLevelsAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            logger.LogDebug("Ignoring duplicate released-level load while another load is already running.");
            return;
        }

        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("level-selection.load-levels", ActivityKind.Internal);
        activity?.SetTag("network.online", networkStatus.HasInternetAccess);

        var stopwatch = Stopwatch.StartNew();
        IsBusy = true;
        LoadErrorMessage = string.Empty;
        logger.LogInformation("Loading released levels. Online={HasInternetAccess}.", networkStatus.HasInternetAccess);

        try
        {
            IReadOnlyCollection<ReleasedLevelSummaryDto> releasedLevels;

            if (networkStatus.HasInternetAccess)
            {
                try
                {
                    releasedLevels = await levelsApiService.GetReleasedLevelsAsync(cancellationToken);
                    await CacheReleasedLevelsAsync(releasedLevels, cancellationToken);
                    ApplyLevels(releasedLevels);
                    activity?.SetTag("levels.source", "server");
                    activity?.SetTag("levels.count", releasedLevels.Count);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    logger.LogInformation("Loaded {Count} released levels from the FloodRush API.", releasedLevels.Count);
                    return;
                }
                catch (HttpRequestException exception)
                {
                    logger.LogWarning(exception, "Refreshing released levels from the server failed. Falling back to the local cache.");
                    releasedLevels = await levelCacheService.GetReleasedLevelsAsync(cancellationToken);
                    if (releasedLevels.Count == 0)
                    {
                        Levels.Clear();
                        LoadErrorMessage = "Couldn't reach the FloodRush API and no cached levels are available.";
                        activity?.SetStatus(ActivityStatusCode.Error, LoadErrorMessage);
                        return;
                    }

                    ApplyLevels(releasedLevels);
                    LoadErrorMessage = "Couldn't update levels from the server. Showing cached levels.";
                    activity?.SetTag("levels.source", "cache");
                    activity?.SetTag("levels.count", releasedLevels.Count);
                    activity?.SetTag("levels.cache_fallback", true);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    logger.LogInformation("Loaded {Count} released levels from the local cache after a server refresh failure.", releasedLevels.Count);
                    return;
                }
            }

            releasedLevels = await levelCacheService.GetReleasedLevelsAsync(cancellationToken);
            if (releasedLevels.Count == 0)
            {
                Levels.Clear();
                LoadErrorMessage = "No cached levels are available while offline.";
                activity?.SetStatus(ActivityStatusCode.Error, LoadErrorMessage);
                return;
            }

            ApplyLevels(releasedLevels);
            LoadErrorMessage = "You're offline. Showing cached levels.";
            activity?.SetTag("levels.source", "cache");
            activity?.SetTag("levels.count", releasedLevels.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Loaded {Count} released levels from the local cache while offline.", releasedLevels.Count);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Canceled released-level loading.");
        }
        catch (InvalidOperationException exception)
        {
            Levels.Clear();
            LoadErrorMessage = exception.Message;
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Loading released levels failed due to invalid local or remote data.");
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "released-levels-load" }
            });
            IsBusy = false;
        }
    }

    public bool ConsumeRefreshRequest()
    {
        var shouldRefresh = shouldRefreshOnNextAppearance;
        shouldRefreshOnNextAppearance = false;
        return shouldRefresh;
    }

    private void ApplyLevels(IReadOnlyCollection<ReleasedLevelSummaryDto> releasedLevels)
    {
        LoadErrorMessage = string.Empty;
        Levels.Clear();
        foreach (var releasedLevel in releasedLevels)
        {
            Levels.Add(new LevelListItem(
                releasedLevel.LevelId,
                releasedLevel.DisplayName,
                releasedLevel.Difficulty,
                releasedLevel.FlowSpeedIndicator,
                false));
        }
    }

    private async Task CacheReleasedLevelsAsync(
        IReadOnlyCollection<ReleasedLevelSummaryDto> releasedLevels,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Caching {Count} released levels for offline use.", releasedLevels.Count);

        foreach (var releasedLevel in releasedLevels)
        {
            var revision = await levelsApiService.GetLevelRevisionAsync(
                releasedLevel.LevelId,
                releasedLevel.Revision,
                cancellationToken);

            await levelCacheService.SaveLevelRevisionAsync(revision, cancellationToken);
        }

        await levelCacheService.SaveReleasedLevelsAsync(releasedLevels, cancellationToken);
    }

    private void RecordUserAction(string action)
    {
        FloodRushTelemetry.UserActions.Add(1, new TagList
        {
            { "screen", "level-selection" },
            { "action", action }
        });
    }
}

public sealed record LevelListItem(
    string LevelId,
    string DisplayName,
    string Difficulty,
    int FlowSpeedIndicator,
    bool IsCompleted);
