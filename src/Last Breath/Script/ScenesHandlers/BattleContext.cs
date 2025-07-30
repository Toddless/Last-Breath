namespace Playground.Script.ScenesHandlers
{
    using System.Collections.Generic;

    public class BattleContext(List<ICharacter> opponents) : IBattleContext
    {
        public List<ICharacter> Fighters { get; private set; } = opponents;
    }
}
