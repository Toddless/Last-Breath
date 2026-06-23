namespace Battle.Source.Abilities.IncreasingPressure
{
    using Godot;
    using System.Linq;
    using System.Threading;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Utilities;

    public class IpSingleAttackExecutionStrategy(List<IAttackModifier> modifiers) : IIpExecutionStrategy
    {
        public List<IAttackModifier> Modifiers { get; } = modifiers;

        public void AddAttackModifier(IAttackModifier modifier)
        {
            if (Modifiers.FirstOrDefault(mod => mod.Id == modifier.Id) == null)
                Modifiers.Add(modifier);
        }

        public void RemoveAttackModifier(IAttackModifier modifier) => Modifiers.Remove(modifier);

        public async Task Execute(IncreasingPressure ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            var cts = new CancellationTokenSource();
            rnd.Randomize();

            foreach (IEntity target in targets)
            {
                float totalDamage = CalculateTotalDamage(ability, owner);
                var scheduler = new AttackContextScheduler();
                var meta = new AttackMetadata(0, 1);
                var context = new AttackContext(owner, target, totalDamage, rnd, scheduler)
                {
                    RawCriticalDamage = owner.Parameters.CriticalDamage, RawCriticalChance = owner.Parameters.CriticalChance,
                };

                foreach (IAttackModifier attackModifier in Modifiers)
                    attackModifier.Apply(context, meta);

                if (!context.Schedule()) break;
                await scheduler.DrainQueue(cts.Token);
            }
        }

        private float CalculateTotalDamage(IncreasingPressure ability, IEntity owner)
        {
            float increase = 1f;
            float totalDamage = 0f;
            for (int i = 0; i < ability.Attacks; i++)
            {
                float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                float damage = (owner.Parameters.Damage + additionalDamage) * increase;
                totalDamage += damage;
                increase += ability.IncreaseAttackDamage;
            }

            return totalDamage;
        }
    }
}
