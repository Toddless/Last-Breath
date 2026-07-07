namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;

    /// <summary>Published when a multicast (Intelligence stance) ability resolves its activation stage.</summary>
    public record AbilityStageActivatedEvent(IAbility Ability, int Stage) : ICombatEvent;
}
