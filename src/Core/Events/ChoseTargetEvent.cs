namespace Core.Events
{
    using System.Collections.Generic;
    using Battle;
    using Entity;

    public record ChoseTargetEvent( List<IFightable> Targets) : ICombatEvent
    {
    }
}
