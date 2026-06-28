namespace Battle.Source.Abilities.DarkShroud
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface IDarkShroudExecutionStrategy
    {
        Task Execute(List<IEntity> targets, IEntity owner, IBattleField field);
    }
}
