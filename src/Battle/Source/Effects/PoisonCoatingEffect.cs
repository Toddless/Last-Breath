namespace Battle.Source.Effects
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// Buff applied to the caster. Each attack made while this effect is active applies a poison stack to the target.
    /// </summary>
    /// <param name="stacksPerLivingEnemyOn">The field the enemies are counted on when the coating lays a
    /// stack for each of them instead of one; nothing = the plain single stack. Counting needs a field
    /// and an effect has no other road to one, so the cast hands it over.</param>
    public class PoisonCoatingEffect(
        int duration,
        int maxStacks,
        int poisonDuration,
        EffectValue poisonDamagePercent,
        IBattleField? stacksPerLivingEnemyOn = null,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: EffectId, duration, maxStacks, statusEffect)
    {
        public const string EffectId = "Effect_Poison_Coating";

        public int PoisonDuration { get; } = poisonDuration;
        /// <summary>Share of a blow one poison tick carries, AS AUTHORED: the coating hands it to the
        /// poison, and the poison scales it with the effectiveness the coating passes along. The cast
        /// that lays the coating names it — there is nothing to fall back on.</summary>
        public EffectValue PoisonDamagePercent { get; } = poisonDamagePercent;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Target == null) return;
            if (evt.Context.Result != AttackResults.Succeed) return;

            int stacks = StacksPerBlow();
            for (int stack = 0; stack < stacks; stack++) LayPoisonOn(evt);
        }

        /// <summary>One stack, or one for every living enemy where the coating was bought that way. Counted
        /// as the blow lands rather than at the cast: enemies join and fall while the coating holds.</summary>
        private int StacksPerBlow() =>
            stacksPerLivingEnemyOn == null || Target == null
                ? 1
                : Math.Max(1, stacksPerLivingEnemyOn.GetEnemies(Target).Count(enemy => enemy.IsAlive));

        private void LayPoisonOn(AfterAttackEvent evt)
        {
            var poison = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison, DamageOverTurnEffect.NoCeilingOfItsOwn, PoisonDamagePercent);
            var applyContext = new EffectApplyingContext
            {
                Caster = Target!,
                Target = evt.Context.Target,
                Source = InstanceId,
                Damage = evt.Context.FinalDamage,
                IsCritical = evt.Context.IsCritical,
                // The coating lays the poison, so the poison lands as hard as the cast that put the
                // coating on — and belongs to that cast, which may therefore go on prolonging it.
                Effectiveness = Effectiveness,
                Trace = Trace
            };
            _ = poison.Apply(applyContext);
        }

        public override IEffect Copy() =>
            new PoisonCoatingEffect(Duration, MaxStacks, PoisonDuration, PoisonDamagePercent, stacksPerLivingEnemyOn, Status);
    }
}
