namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface IIpExecutionStrategy
    {
        Task Execute(IncreasingPressure ability, IEntity owner, List<IEntity> targets, IBattleField field);
    }
}
