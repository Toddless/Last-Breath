namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;

    public class IpSingleAttackExecutionStrategy(List<IAttackModifier> modifiers) : IIpExecutionStrategy
    {
        public List<IAttackModifier> Modifiers { get; } = modifiers;

        public void AddAttackModifier(IAttackModifier modifier)
        {
            if (Modifiers.FirstOrDefault(mod => mod.Id == modifier.Id) == null)
                Modifiers.Add(modifier);
        }

        public void RemoveAttackModifier(IAttackModifier modifier) => Modifiers.Remove(modifier);

        public Task Execute(IncreasingPressure ability, IEntity owner, List<IEntity> targets)
        {
        }
    }
}
