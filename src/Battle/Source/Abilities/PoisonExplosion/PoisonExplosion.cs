namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Conditions;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Removes all poison stacks from the target and instantly deals their accumulated damage.
    /// Executes the target if it had more than <see cref="ExecutionThreshold"/> stacks.
    /// </summary>
    public class PoisonExplosion : Ability
    {
        public PoisonExplosion(AbilityBaseData data) : base(data)
        {
            ExecuteCondition = new PoisonStackExecuteCondition(() => ExecutionThreshold);
        }

        public int ExecutionThreshold => (int)this[AbilityParameter.ExecutionThreshold];
        public float DamageMultiplier => this[AbilityParameter.DamageMultiplier];

        public IPoisonSpreadMode? SpreadMode { get; set; }
        public IExecuteCondition? ExecuteCondition { get; set; }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.DamageMultiplier, 0f);
            parameters.RegisterDefault(AbilityParameter.ExecutionThreshold, 42);
        }

        public override IAbility Copy() => CopyUpgradesTo(new PoisonExplosion(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in targets)
                await ExplodePoison(target, owner, field);
        }

        private async Task ExplodePoison(IFightable target, IFightable owner, IBattleField field)
        {
            var poisonStacks = target.Effects
                .GetBy(e => e.Status == StatusEffects.Poison)
                .OfType<DamageOverTurnEffect>()
                .ToList();

            if (poisonStacks.Count == 0) return;

            // Sum all remaining damage (DamagePerTick × remaining Duration) for each stack
            float totalDamage = poisonStacks.Sum(dot => dot.DamagePerTick * dot.Duration) * (1 + DamageMultiplier);

            var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
            context.Add(DamageType.Poison, totalDamage);
            await target.TakeDamage(context);

            // Execute target if stack count exceeds threshold
            if (ExecuteCondition?.ShouldExecute(target) == true)
                target.Kill();

            // The cast reached a target of its own list and set his poison off on him — the same road
            // every other direct delivery walks, so the same kind. It carried the whole burst and could
            // even execute him, and until this call it was the one delivery that told no rider anything.
            await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, IsCritical: false, context.TotalDamage)
            {
                Source = this,
                Kind = ImpactKind.Hit
            });

            foreach (IFightable caught in SpreadMode?.SpreadPoison(poisonStacks, target, owner, field, InstanceId) ?? [])
                // Nobody aimed at him: he caught the poison only because somebody else's stacks went off,
                // which is what splash is for. No damage of its own — a landing does not need one.
                await ApplyImpactRiders(new AbilityImpact(owner, caught, field, Succeeded: true, IsCritical: false, Damage: 0)
                {
                    Source = this,
                    Kind = ImpactKind.Splash
                });

            foreach (var stack in poisonStacks)
                stack.Remove();
        }
    }
}
