using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;

public sealed record GetReleasedLevelsQuery(string ProfileId) : IQuery<ReleasedLevelsResponse>;
