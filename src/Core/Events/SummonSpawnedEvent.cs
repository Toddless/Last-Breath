namespace Core.Events
{
    using Entity;

    /// <summary>A summon joined the ongoing battle (battle bus): the context mirrors the
    /// latecomer path and creates its HUD bars.</summary>
    public record SummonSpawnedEvent(IFightable Summon, IFightable Summoner) : IBattleEvent;
}
