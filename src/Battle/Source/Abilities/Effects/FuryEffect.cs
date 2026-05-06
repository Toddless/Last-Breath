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
            if (AppliedTo == null) return;
            float healthToBurn = AppliedTo.Parameters.MaxHealth * HealthPercent;
            float currentHealth = AppliedTo.CurrentHealth;
            float toBurn = Mathf.Min(healthToBurn, currentHealth - 1);
            HealthBurned = toBurn;
            AppliedTo.TakeDamage(AppliedTo, toBurn, Status.GetDamageType(), DamageSource.Effect);
            if ((currentHealth - toBurn) <= 1) AppliedTo.Effects.RemoveEffect(this);
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, HealthPercent);

        public override IEffect Clone() => new FuryEffect(Duration, MaxStacks, HealthPercent, Status);
    }
}
