using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Features.GetLevelRevision;

public sealed record GetLevelRevisionQuery(string ProfileId, string LevelId, string Revision) : IQuery<LevelRevisionDto?>;
