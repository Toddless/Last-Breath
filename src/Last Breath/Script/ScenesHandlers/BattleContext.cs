namespace LastBreath.Script.ScenesHandlers
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class BattleContext(List<ICharacter> opponents) : IBattleContext
    {
        public List<ICharacter> Fighters { get; private set; } = opponents;
    }
}
