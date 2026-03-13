using Azure.Data.Tables;
using HexMaster.FloodRush.Server.Abstractions.Storage;
using HexMaster.FloodRush.Shared.Contracts.Scores;
using Microsoft.Extensions.Configuration;

namespace HexMaster.FloodRush.Server.Scores.Data;

internal sealed class TableScoresRepository : IScoresRepository
{
    private const string TableName = "scores";
    private readonly TableClient tableClient;

    public TableScoresRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(StorageResourceNames.Tables)
            ?? throw new InvalidOperationException(
                $"Connection string '{StorageResourceNames.Tables}' is required for the scores module.");

        tableClient = new TableClient(connectionString, TableName);
    }

    public async ValueTask<LevelScoreDto> SubmitScoreAsync(
        string profileId,
        SubmitScoreRequest request,
        CancellationToken cancellationToken)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var entity = new LevelScoreEntity
        {
            PartitionKey = request.LevelId,
            RowKey = $"{Guid.NewGuid():N}",
            LevelRevision = request.LevelRevision,
            ProfileId = profileId,
            Points = request.Points,
            AchievedAtUtc = request.AchievedAtUtc
        };

        await tableClient.AddEntityAsync(entity, cancellationToken);

        return entity.ToDto();
    }

    public async ValueTask<IReadOnlyCollection<LevelScoreDto>> GetTopScoresAsync(
        string levelId,
        int take,
        CancellationToken cancellationToken)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var scores = new List<LevelScoreDto>();
        var query = tableClient.QueryAsync<LevelScoreEntity>(
            entity => entity.PartitionKey == levelId,
            cancellationToken: cancellationToken);

        await foreach (var entity in query)
        {
            scores.Add(entity.ToDto());
        }

        return scores
            .OrderByDescending(score => score.Points)
            .ThenBy(score => score.AchievedAtUtc)
            .Take(take)
            .ToArray();
    }

    public async ValueTask<LevelScoreDto?> GetPlayerBestScoreAsync(
        string profileId,
        string levelId,
        CancellationToken cancellationToken)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var scores = new List<LevelScoreDto>();
        var query = tableClient.QueryAsync<LevelScoreEntity>(
            entity => entity.PartitionKey == levelId && entity.ProfileId == profileId,
            cancellationToken: cancellationToken);

        await foreach (var entity in query)
        {
            scores.Add(entity.ToDto());
        }

        return scores
            .OrderByDescending(s => s.Points)
            .ThenBy(s => s.AchievedAtUtc)
            .FirstOrDefault();
    }
}
