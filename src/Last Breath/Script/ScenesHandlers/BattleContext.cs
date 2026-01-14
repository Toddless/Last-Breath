namespace LastBreath.Script.ScenesHandlers
{
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class BattleContext(List<IEntity> opponents) : IBattleContext
    {
        public List<IEntity> Fighters { get; private set; } = opponents;
    }
}
