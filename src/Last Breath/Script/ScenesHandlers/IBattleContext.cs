namespace LastBreath.Script.ScenesHandlers
{
    using System.Collections.Generic;
    using LastBreath.Script;

    public interface IBattleContext
    {
        List<ICharacter> Fighters { get; }
    }
}
