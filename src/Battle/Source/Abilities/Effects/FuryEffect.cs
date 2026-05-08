namespace Battle.Source.Abilities.Effects
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
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

        public override void BeforeAttack(IAttackContext context)
        {
            if (Target == null) return;
            float healthToBurn = Target.Parameters.MaxHealth * HealthPercent;
            float currentHealth = Target.CurrentHealth;
            float toBurn = Mathf.Min(healthToBurn, currentHealth - 1);
            HealthBurned = toBurn;
            Target.TakeDamage(new DamageContext { Source = Target, Damage = toBurn, Cause = DamageCause.Effect, Type = Status.GetDamageType() });
            if ((currentHealth - toBurn) <= 1) Target.Effects.RemoveEffect(this);
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, HealthPercent);

        public override IEffect Copy() => new FuryEffect(Duration, MaxStacks, HealthPercent, Status);
    }
}
