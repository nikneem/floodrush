using HexMaster.FloodRush.Game.Core.Domain.Pipes;
using HexMaster.FloodRush.Game.Core.Presentation.Gameplay;

namespace HexMaster.FloodRush.Game.Core.Tests.Presentation;

public sealed class PipeStackGeneratorTests
{
    [Fact]
    public void GenerateInitialStack_ReturnsRequestedCount()
    {
        var stack = PipeStackGenerator.GenerateInitialStack(10, 1234);

        Assert.Equal(10, stack.Count);
    }

    [Fact]
    public void GenerateInitialStack_UsesOnlyProvidedPipeTypes()
    {
        var allowedPipeTypes = new[]
        {
            PipeSectionType.Horizontal,
            PipeSectionType.Cross
        };

        var stack = PipeStackGenerator.GenerateInitialStack(
            10,
            1234,
            allowedPipeTypes);

        Assert.All(stack, pipeType => Assert.Contains(pipeType, allowedPipeTypes));
    }

    [Fact]
    public void GenerateInitialStack_ReturnsSameSequenceForSameSeed()
    {
        var first = PipeStackGenerator.GenerateInitialStack(10, 1234);
        var second = PipeStackGenerator.GenerateInitialStack(10, 1234);

        Assert.Equal(first, second);
    }
}
