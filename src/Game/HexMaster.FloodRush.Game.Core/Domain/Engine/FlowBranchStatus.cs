namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

public enum FlowBranchStatus
{
    /// <summary>The branch is actively advancing through the board.</summary>
    Active = 0,

    /// <summary>The branch successfully reached a finish point.</summary>
    Completed = 1,

    /// <summary>The branch hit a dead end, an invalid connection, or exited the board.</summary>
    Failed = 2
}
