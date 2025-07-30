namespace Playground.Script.ScenesHandlers
{
    using System.Collections.Generic;

    public interface IBattleContext
    {
        List<ICharacter> Fighters { get; }
    }
}
