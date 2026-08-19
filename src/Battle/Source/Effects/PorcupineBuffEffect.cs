namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// The Porcupine buff: every real enemy hit is answered with sacred damage — a share of the damage
    /// taken plus a share of the bearer's armor. Optional hooks (upgrades): heal on being hit and a
    /// chance to shave a turn off the source ability's cooldown.
    /// </summary>
    public class PorcupineBuffEffect(
        IAbility sourceAbility,
        int duration,
        EffectValue damageReturn,
        EffectValue armorReturn,
        EffectValue healOnHitPercent,
        EffectValue cooldownReduceChance)
        : Effect(id: "Effect_Porcupine", duration, maxStacks: 1)
    {
        /// <summary>Share of the damage taken that goes back at the attacker.</summary>
        public float DamageReturn => Effective(damageReturn);

        /// <summary>Share of the bearer's armor added to the answer.</summary>
        public float ArmorReturn => Effective(armorReturn);

        /// <summary>Share of maximum health the bearer gets back on being hit.</summary>
        public float HealOnHitPercent => Effective(healOnHitPercent);

        /// <summary>Odds of shaving a turn off the source ability's cooldown.</summary>
        public float CooldownReduceChance => Effective(cooldownReduceChance);

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            SubscribeUntilRemoved<DamageTakenEvent>(Target.CombatEvents, OnDamageTaken);
        }

        public override bool IsStronger(IEffect otherEffect) =>
            otherEffect is PorcupineBuffEffect other && DamageReturn > other.DamageReturn;

        public override IEffect Copy() =>
            new PorcupineBuffEffect(sourceAbility, Duration, damageReturn, armorReturn, healOnHitPercent, cooldownReduceChance);

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (Target == null) return;
            var context = evt.Context;
            // Answer only real hits: reflected damage, DoTs and passives must not ping-pong.
            if (context.Cause is not (DamageCause.Attack or DamageCause.Ability)) return;
            var attacker = context.Source;
            if (attacker.IsSame(Target.InstanceId) || !attacker.IsAlive) return;

            if (HealOnHitPercent > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * HealOnHitPercent });
            if (CooldownReduceChance > 0 && sourceAbility.CooldownLeft > 0 && ChanceRoll.Roll(CooldownReduceChance, CombatRandom.Rolls))
                sourceAbility.CooldownLeft--;

            float returned = (context.TotalDamage * DamageReturn) + (Target.Parameters.Armor * ArmorReturn);
            if (returned <= 0) return;
            var retaliation = new DamageContext { Source = Target, Cause = DamageCause.Effect };
            retaliation.Add(DamageType.Sacred, returned);
            attacker.TakeDamage(retaliation);
        }
    }
}
