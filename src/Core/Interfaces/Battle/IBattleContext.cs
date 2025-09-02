namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Core.Interfaces;

    public interface IBattleContext
    {
        List<ICharacter> Fighters { get; }
    }
}
