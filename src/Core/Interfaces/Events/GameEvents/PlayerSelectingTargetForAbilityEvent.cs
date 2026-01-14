namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;

    public record PlayerSelectingTargetForAbilityEvent(IAbility Ability, string SelectionId) : IBattleEvent, ICombatEvent;
}
