namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;
    using Godot;
    using Utilities;

    public class FuryEffect(
        int duration,
        int maxStacks,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect_Fury")
        : Effect(id, duration, maxStacks, statusEffect)
    {
        protected float HealthBurned;
        public float HealthPercent { get; } = healthPercent;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<BeforeAttackEvent>(Target.CombatEvents, OnBeforeAttack);
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        protected virtual void OnBeforeAttack(BeforeAttackEvent evt)
        {
            if (Target == null) return;
            float healthToBurn = Target.Parameters.MaxHealth * HealthPercent;
            float currentHealth = Target.CurrentHealth;
            float toBurn = Mathf.Min(healthToBurn, currentHealth - 1);
            HealthBurned = toBurn;
            var damageContext = new DamageContext { Source = Target, Cause = DamageCause.Effect };
            damageContext.Add(Status.GetDamageType(), toBurn);
            Target.TakeDamage(damageContext);
            if ((currentHealth - toBurn) <= 1) Remove();
        }

        protected virtual void OnAfterAttack(AfterAttackEvent evt)
        {
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, HealthPercent);

        public override IEffect Copy() => new FuryEffect(Duration, MaxStacks, HealthPercent, Status);
    }
}
