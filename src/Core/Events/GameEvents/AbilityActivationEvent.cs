namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;

    public record AbilityActivationEvent(IAbility Ability, string SelectionId) : ICombatEvent, IGameEvent, IBattleEvent;
}
