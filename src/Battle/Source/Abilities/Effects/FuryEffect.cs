namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;

    public class FuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        protected float HealthBurned;
        public float HealthPercent { get; } = healthPercent;

        public override void BeforeAttack(IEntity source, IAttackContext context)
        {
            float healthToBurn = source.Parameters.MaxHealth * HealthPercent;
            float currentHealth = source.CurrentHealth;
            float toBurn = Mathf.Min(healthToBurn, currentHealth - 1);
            HealthBurned = toBurn;
            source.TakeDamage(source, toBurn, Status.GetDamageType(), DamageSource.Effect);
            if ((currentHealth - toBurn) <= 1) source.Effects.RemoveEffect(this);
        }

        public override IEffect Clone() => new FuryEffect(Id, Duration, HealthPercent, Status);
    }
}
