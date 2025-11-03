namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Core.Interfaces.Entity;

    public interface IBattleContext
    {
        List<IEntity> Fighters { get; }
    }
}
