namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Core.Interfaces.Entity;

    public interface IBattleField
    {
        IReadOnlyList<IEntity> GetEnemies(IEntity entity);
        IReadOnlyList<IEntity> GetAllies(IEntity entity);
        IReadOnlyList<IEntity> GetAll();
        IEntity GetRandomEntity(IEntity entity);
        IEntity GetRandomAlly(IEntity entity);
    }
}
