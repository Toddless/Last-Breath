namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Conditions;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Effects;

    /// <summary>
    /// Removes all poison stacks from the target and instantly deals their accumulated damage.
    /// Executes the target if it had more than <see cref="ExecutionThreshold"/> stacks.
    /// </summary>
    public class PoisonExplosion : AttackAbility
    {
        public PoisonExplosion(string[] tags,
            int cooldown,
            int costValue,
            int executionThreshold,
            float damageMultiplier,
            Costs costType = Costs.Mana) : base(id: "Ability_Poison_Explosion", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0, costType)
        {
            ExecutionThreshold = executionThreshold;
            DamageMultiplier = damageMultiplier;
            ExecuteCondition = new PoisonStackExecuteCondition(() => ExecutionThreshold);
        }

        public int ExecutionThreshold { get; set; }
        public float DamageMultiplier { get; set; }

        /// <summary>When true (L3 upgrade), stacks are NOT removed — damage is dealt while keeping them.</summary>
        public bool PreserveStacks { get; set; }

        public IPoisonSpreadMode? SpreadMode { get; set; }
        public IExecuteCondition? ExecuteCondition { get; set; }

        public override IAbility Copy()
        {
            var copy = new PoisonExplosion(Tags, (int)Cooldown, CostValue, ExecutionThreshold, DamageMultiplier, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field)
        {
            foreach (IEntity target in targets)
                await ExplodePoison(target, owner, field);
        }

        private async Task ExplodePoison(IEntity target, IEntity owner, IBattleField field)
        {
            var poisonStacks = target.Effects
                .GetBy(e => e.Status == StatusEffects.Poison)
                .OfType<DamageOverTurnEffect>()
                .ToList();

            if (poisonStacks.Count == 0) return;

            SpreadMode?.SpreadPoison(poisonStacks, target, owner, field, InstanceId);

            // Sum all remaining damage (DamagePerTick × remaining Duration) for each stack
            float totalDamage = poisonStacks.Sum(s => s.DamagePerTick * s.Duration) * DamageMultiplier;

            if (!PreserveStacks)
            {
                foreach (var stack in poisonStacks)
                    stack.Remove();
            }

            var context = new DamageContext { Source = owner, Damage = totalDamage, Type = DamageType.Normal, Cause = DamageCause.Ability };
            target.TakeDamage(context);

            // Execute target if stack count exceeds threshold
            if (ExecuteCondition?.ShouldExecute(target) == true)
                target.Kill();
        }
    }
}
