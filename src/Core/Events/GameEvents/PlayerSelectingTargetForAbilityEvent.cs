namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;

    public record PlayerSelectingTargetForAbilityEvent(IAbility Ability, string SelectionId) : IBattleEvent, ICombatEvent;
}
