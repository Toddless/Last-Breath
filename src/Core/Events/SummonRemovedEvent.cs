namespace Core.Events
{
    using Entity;

    /// <summary>A summon leaves the field for good — its slot is gone and its node is about to be
    /// freed (battle bus). The mirror of <see cref="SummonSpawnedEvent"/>: everything the spawn
    /// created for that body — the HUD bars first — is torn down here, BEFORE the free, so nothing
    /// keeps a reference that outlives the node.</summary>
    public record SummonRemovedEvent(IFightable Summon) : IBattleEvent;
}
