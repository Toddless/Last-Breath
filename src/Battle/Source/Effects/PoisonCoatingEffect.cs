namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events.GameEvents;

    /// <summary>
    /// Buff applied to the caster. Each attack made while this effect is active applies a poison stack to the target.
    /// </summary>
    public class PoisonCoatingEffect(
        int duration,
        int maxStacks,
        int poisonDuration,
        float poisonDamagePercent = 0.7f,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: EffectId, duration, maxStacks, statusEffect)
    {
        public const string EffectId = "Effect_Poison_Coating";

        public int PoisonDuration { get; } = poisonDuration;
        public float PoisonDamagePercent { get; } = poisonDamagePercent;

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
                IsCritical = evt.Context.IsCritical
            };
            poison.Apply(applyContext);
        }

        public override IEffect Copy() =>
            new PoisonCoatingEffect(Duration, MaxStacks, PoisonDuration, PoisonDamagePercent, Status);
    }
}
