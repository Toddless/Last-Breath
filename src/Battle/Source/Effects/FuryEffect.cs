namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Godot;

    public class FuryEffect(
        int duration,
        int maxStacks,
        EffectValue healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect_Fury")
        : Effect(id, duration, maxStacks, statusEffect)
    {
        protected float HealthBurned;

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(HealthPercent)] = HealthPercent * 100f;
                return values;
            }
        }

        /// <summary>Share of maximum health every attack eats. Deliberately NOT read through
        /// <c>Effective</c>: this is the fury's PRICE, and strengthening a buff must not turn on its
        /// bearer. It also keeps the Burning Fury linear — the burn it lays is a share of the health
        /// burned here, so scaling both ends would square the effectiveness.</summary>
        public float HealthPercent => healthPercent.Authored;

        /// <summary>What the variants hand on to their own copies.</summary>
        protected EffectValue AuthoredHealthPercent => healthPercent;

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

        public override IEffect Copy() => new FuryEffect(Duration, MaxStacks, healthPercent, Status);
    }
}
