namespace Battle.Source.Abilities.IncreasingPressure
{
    using Godot;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using System.Threading;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using System.Collections.Generic;

    public class IpDefaultExecutionStrategy(List<IAttackModifier> modifiers) : IIpExecutionStrategy
    {
        public List<IAttackModifier> Modifiers { get; } = modifiers;

        public void AddAttackModifier(IAttackModifier modifier)
        {
            if (Modifiers.FirstOrDefault(mod => mod.Id == modifier.Id) == null)
                Modifiers.Add(modifier);
        }

        public void RemoveAttackModifier(IAttackModifier modifier) => Modifiers.Remove(modifier);

        public virtual async Task Execute(IncreasingPressure ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            var cts = new CancellationTokenSource();
            rnd.Randomize();

            foreach (IEntity target in targets)
            {
                var scheduler = new AttackContextScheduler();
                float increase = 1f;
                for (int i = 0; i < ability.Attacks; i++)
                {
                    if (!target.IsAlive) break;
                    var meta = new AttackMetadata(i, (int)ability.Attacks);
                    float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                    additionalDamage *= increase;
                    float damage = owner.Parameters.Damage * increase;
                    var context = new AttackContext(owner, target, damage, rnd, scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance, RawCriticalDamage = owner.Parameters.CriticalDamage, AdditionalDamage = additionalDamage
                    };

                    foreach (var modifier in Modifiers)
                        modifier.Apply(context, meta);

                    if (!context.Schedule()) break;
                    await scheduler.DrainQueue(cts.Token);

                    if (context.Result is AttackResults.Succeed) increase += ability.IncreaseAttackDamage;
                }
            }
        }
    }
}
