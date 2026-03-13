using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Presentation.Gameplay;

public static class PipeStackGenerator
{
    private static readonly PipeSectionType[] DefaultPipeTypes = Enum.GetValues<PipeSectionType>();

    public static IReadOnlyList<PipeSectionType> GenerateInitialStack(
        int count,
        int seed,
        IReadOnlyCollection<PipeSectionType>? availablePipeTypes = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var pipeTypes = (availablePipeTypes ?? DefaultPipeTypes)
            .Distinct()
            .ToArray();

        if (pipeTypes.Length == 0)
        {
            throw new ArgumentException("At least one pipe type must be available.", nameof(availablePipeTypes));
        }

        foreach (var pipeType in pipeTypes)
        {
            if (!Enum.IsDefined(pipeType))
            {
                throw new ArgumentOutOfRangeException(nameof(availablePipeTypes), "Unknown pipe section type.");
            }
        }

        var random = new Random(seed);
        var stack = new PipeSectionType[count];

        for (var index = 0; index < count; index++)
        {
            stack[index] = pipeTypes[random.Next(pipeTypes.Length)];
        }

        return stack;
    }
}
