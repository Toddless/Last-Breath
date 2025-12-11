namespace Battle.Source.CombatEvents
{
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record ChoseTargetEvent(IEntity Source, List<IEntity> Targets) : ICombatEvent
    {
    }
}
