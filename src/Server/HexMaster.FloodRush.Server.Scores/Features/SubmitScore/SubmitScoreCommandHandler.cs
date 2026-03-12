using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Scores.Data;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Features.SubmitScore;

internal sealed class SubmitScoreCommandHandler(IScoresRepository repository)
    : ICommandHandler<SubmitScoreCommand, LevelScoreDto>
{
    public async ValueTask<LevelScoreDto> HandleAsync(
        SubmitScoreCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.LevelId))
        {
            throw new ArgumentException("LevelId is required.", nameof(command.Request.LevelId));
        }

        if (string.IsNullOrWhiteSpace(command.Request.LevelRevision))
        {
            throw new ArgumentException("LevelRevision is required.", nameof(command.Request.LevelRevision));
        }

        if (command.Request.Points < 0)
        {
            throw new ArgumentException("Points must be zero or greater.", nameof(command.Request.Points));
        }

        return await repository.SubmitScoreAsync(command.ProfileId, command.Request, cancellationToken);
    }
}
