namespace Core.Interfaces.Events.GameEvents
{
    using System.Collections.Generic;
    using Abilities;
    using Battle;
    using Entity;

    public record AbilityActivatedEvent(IAbility Ability, List<IEntity> Targets) : ICombatEvent, IGameEvent, IBattleEvent;
}
