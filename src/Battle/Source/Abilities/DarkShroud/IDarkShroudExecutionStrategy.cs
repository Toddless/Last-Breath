namespace Battle.Source.Abilities.DarkShroud
{
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public interface IDarkShroudExecutionStrategy
    {
        Task Execute(List<IEntity> targets, IEntity owner, IBattleField field);
    }
}
