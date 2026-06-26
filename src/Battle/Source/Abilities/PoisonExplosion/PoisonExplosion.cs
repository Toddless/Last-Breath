namespace Battle.Source.Abilities.PoisonExplosion
{
    using Effects;
    using Core.Enums;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;

    /// <summary>
    /// Removes all poison stacks from the target and instantly deals their accumulated damage.
    /// Executes the target if it had more than <see cref="ExecutionThreshold"/> stacks.
    /// </summary>
    public class PoisonExplosion(
        string[] tags,
        int cooldown,
        int costValue,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        int executionThreshold,
        float damageMultiplier,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Poison_Explosion",
            tags,
            cooldown,
            costValue,
            damage: 0,
            weaponDamageScale: 0,
            spellDamageScale: 0,
            upgrades,
            costType)
    {
        public int ExecutionThreshold { get; set; } = executionThreshold;
        public float DamageMultiplier { get; set; } = damageMultiplier;

        /// <summary>When true (L3 upgrade), stacks are NOT removed — damage is dealt while keeping them.</summary>
        public bool PreserveStacks { get; set; } = false;

        /// <summary>When true (L3 upgrade), spreads poison to all enemies instead of exploding on one.</summary>
        public bool SpreadMode { get; set; } = false;

        public override IAbility Copy() => new PoisonExplosion(Tags, (int)Cooldown, CostValue, Upgrades, ExecutionThreshold, DamageMultiplier, CostType);

        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field)
        {
            foreach (IEntity target in targets)
                ExplodePoison(target, owner, field);

            return Task.CompletedTask;
        }

        private void ExplodePoison(IEntity target, IEntity owner, IBattleField field)
        {
            var poisonStacks = target.Effects
                .GetBy(e => e.Status == StatusEffects.Poison)
                .OfType<DamageOverTurnEffect>()
                .ToList();

            if (poisonStacks.Count == 0) return;

            int stackCount = poisonStacks.Count;

            if (SpreadMode)
            {
                SpreadPoisonToAllEnemies(poisonStacks, target, owner, field);
                return;
            }

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
            if (stackCount > ExecutionThreshold)
                target.Kill();
        }

        private void SpreadPoisonToAllEnemies(
            List<DamageOverTurnEffect> originalStacks, IEntity originalTarget, IEntity owner, IBattleField field)
        {
            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive && e != originalTarget).ToList();
            if (enemies.Count == 0) return;

            foreach (IEntity enemy in enemies)
            {
                foreach (var stack in originalStacks)
                {
                    var clone = (DamageOverTurnEffect)stack.Copy();
                    clone.Apply(new EffectApplyingContext { Caster = owner, Target = enemy, Source = InstanceId, Damage = stack.DamagePerTick });
                }
            }
        }
    }
}
