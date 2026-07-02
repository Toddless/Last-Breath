namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface ISoAExecutionStrategy
    {
        Task Execute(SeriesOfAttacks ability, IFightable owner, List<IFightable> targets, IBattleField field);
    }
}
