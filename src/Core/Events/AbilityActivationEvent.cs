namespace Core.Events
{
    using Battle;
    using Battle.Abilities;

    public record AbilityActivationEvent(IAbility Ability, string SelectionId) : ICombatEvent, IGameEvent, IBattleEvent;
}
