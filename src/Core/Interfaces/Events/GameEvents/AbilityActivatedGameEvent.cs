namespace Core.Interfaces.Events.GameEvents
{
    using System.Collections.Generic;
    using Abilities;
    using Battle;
    using Entity;

    public record AbilityActivatedGameEvent(IAbility Ability, List<IEntity> Targets) : ICombatEvent, IGameEvent, IBattleEvent;
}
