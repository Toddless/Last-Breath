namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface IJoPExecutionStrategy
    {
        Task Execute(JarOfPoison ability, IEntity owner, List<IEntity> targets, IBattleField field);
    }
}
