namespace Battle.Source.Abilities.DarkShroud
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Entity;

    public class DefaultExecutionStrategy : IDarkShroudExecutionStrategy
    {
        public Task Execute(List<IFightable> targets, IFightable owner, IBattleField field) => throw new System.NotImplementedException();
    }
}
