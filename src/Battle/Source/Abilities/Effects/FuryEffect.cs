namespace Battle.Source.Abilities.Effects
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

    public class FuryEffect(
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect")
        : Effect(id, duration, maxStacks: 1, statusEffect)
    {
        protected float HealthBurned;
        public float HealthPercent { get; } = healthPercent;

        public override void BeforeAttack(IAttackContext context)
        {
            if (Owner == null) return;
            float healthToBurn = Owner.Parameters.MaxHealth * HealthPercent;
            float currentHealth = Owner.CurrentHealth;
            float toBurn = Mathf.Min(healthToBurn, currentHealth - 1);
            HealthBurned = toBurn;
            Owner.TakeDamage(Owner, toBurn, Status.GetDamageType(), DamageSource.Effect);
            if ((currentHealth - toBurn) <= 1) Owner.Effects.RemoveEffect(this);
        }

        public override IEffect Clone() => new FuryEffect(Duration, HealthPercent, Status);
    }
}
