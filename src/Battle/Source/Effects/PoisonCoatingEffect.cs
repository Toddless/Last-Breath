namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// Buff applied to the caster. Each attack made while this effect is active applies a poison stack to the target.
    /// </summary>
    public class PoisonCoatingEffect(
        int duration,
        int maxStacks,
        int poisonDuration,
        EffectValue poisonDamagePercent = default,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: EffectId, duration, maxStacks, statusEffect)
    {
        public const string EffectId = "Effect_Poison_Coating";

        private const float DefaultPoisonPercent = 0.7f;

        public int PoisonDuration { get; } = poisonDuration;
        /// <summary>Share of a blow one poison tick carries, AS AUTHORED: the coating hands it to the
        /// poison, and the poison scales it with the effectiveness the coating passes along.</summary>
        public EffectValue PoisonDamagePercent { get; } =
            poisonDamagePercent.Authored == 0f ? DefaultPoisonPercent : poisonDamagePercent;

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

            var poison = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison, 999, PoisonDamagePercent);
            var applyContext = new EffectApplyingContext
            {
                Caster = Target,
                Target = evt.Context.Target,
                Source = InstanceId,
                Damage = evt.Context.FinalDamage,
                IsCritical = evt.Context.IsCritical,
                // The coating lays the poison, so the poison lands as hard as the cast that put the
                // coating on — and belongs to that cast, which may therefore go on prolonging it.
                Effectiveness = Effectiveness,
                Trace = Trace
            };
            poison.Apply(applyContext);
        }

        public override IEffect Copy() =>
            new PoisonCoatingEffect(Duration, MaxStacks, PoisonDuration, PoisonDamagePercent, Status);
    }
}
