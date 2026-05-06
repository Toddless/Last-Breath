namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface ISoAExecutionStrategy
    {
        Task Execute(SeriesOfAttacks ability, IEntity owner, List<IEntity> targets, IBattleField field);
    }
}
