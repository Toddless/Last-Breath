namespace Battle.TestData.Abilities.Effects
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

    public class FuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        protected float HealthBurned;
        public float HealthPercent { get; } = healthPercent;

        public override void OnBeforeAttack(IEntity target, IAttackContext context)
        {
            float healthToBurn = target.Parameters.MaxHealth * HealthPercent;
            float currentHealth = target.CurrentHealth;
            float toBurn = Mathf.Min(healthToBurn, currentHealth - 1);
            HealthBurned = toBurn;
            target.TakeDamage(toBurn, Status.GetDamageType(), DamageSource.Effect);
            if ((currentHealth - toBurn) <= 1) target.Effects.RemoveEffect(this);
        }

        public override IEffect Clone() => new FuryEffect(Id, Duration, HealthPercent, Status);
    }
}
