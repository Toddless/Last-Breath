namespace LastBreath.Script.ScenesHandlers
{
    using System.Collections.Generic;
    using Core.Interfaces;

    public interface IBattleContext
    {
        List<ICharacter> Fighters { get; }
    }
}
