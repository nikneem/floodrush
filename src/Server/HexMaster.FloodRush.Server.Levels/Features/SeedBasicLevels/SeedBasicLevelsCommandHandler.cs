using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Levels.Data;

namespace HexMaster.FloodRush.Server.Levels.Features.SeedBasicLevels;

internal sealed class SeedBasicLevelsCommandHandler(ILevelsRepository repository)
    : ICommandHandler<SeedBasicLevelsCommand, SeedBasicLevelsResponse>
{
    public async ValueTask<SeedBasicLevelsResponse> HandleAsync(
        SeedBasicLevelsCommand command,
        CancellationToken cancellationToken)
    {
        var seededCount = await repository.SeedBasicLevelsAsync(cancellationToken);
        return new SeedBasicLevelsResponse(seededCount);
    }
}
