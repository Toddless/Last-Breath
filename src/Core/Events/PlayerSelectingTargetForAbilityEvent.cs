namespace Core.Events
{
    using Battle;
    using Battle.Abilities;

    public record PlayerSelectingTargetForAbilityEvent(IAbility Ability, string SelectionId) : IBattleEvent, ICombatEvent;
}
