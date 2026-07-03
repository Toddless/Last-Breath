namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;

    /// <summary>Published when a multicast (Intelligence stance) ability resolves its activation stage.</summary>
    public record AbilityStageActivatedEvent(IAbility Ability, int Stage) : ICombatEvent;
}
