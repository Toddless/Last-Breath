namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Entity;

    public interface ISoAExecutionStrategy
    {
        Task Execute(SeriesOfAttacks ability, IFightable owner, List<IFightable> targets, IBattleField field);
    }
}
