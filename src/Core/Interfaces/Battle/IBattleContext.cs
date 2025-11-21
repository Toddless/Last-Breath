namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Entity;

    public interface IBattleContext
    {
        List<IEntity> Fighters { get; }
    }
}
