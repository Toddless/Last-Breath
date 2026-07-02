namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Core.Interfaces.Entity;

    public interface IBattleField
    {
        IReadOnlyList<IFightable> GetEnemies(IFightable entity);
        IReadOnlyList<IFightable> GetAllies(IFightable entity);
        IReadOnlyList<IFightable> GetAll();
        IFightable GetRandomEntity(IFightable entity);
        IFightable GetRandomAlly(IFightable entity);
    }
}
