using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Levels;

/// <summary>
/// An immutable snapshot of a <see cref="LevelDefinition"/> tied to a specific
/// <see cref="LevelRevisionToken"/>. Once created, a revision must never change.
/// If the level behaviour changes, publish a new revision.
/// </summary>
public sealed class LevelRevision
{
    public LevelRevisionToken RevisionToken { get; }
    public LevelDefinition Definition { get; }
    public LevelMetadata Metadata { get; }

    public LevelRevision(LevelRevisionToken revisionToken, LevelDefinition definition, LevelMetadata metadata)
    {
        RevisionToken = Guard.AgainstNull(revisionToken, nameof(revisionToken));
        Definition = Guard.AgainstNull(definition, nameof(definition));
        Metadata = Guard.AgainstNull(metadata, nameof(metadata));
    }
}
