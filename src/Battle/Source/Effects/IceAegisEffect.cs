namespace Battle.Source.Effects
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Godot;

    /// <summary>
    /// The Ice Aegis barrier: grants a barrier for a few turns; whatever is left of the grant BURNS on
    /// expiry. The effect itself owns the break reaction — if the bearer's barrier is destroyed before
    /// the effect ends, <paramref name="onBarrierBroken"/> fires (stage 4: freeze). With
    /// <paramref name="attackerEffectFactory"/> set, every real hit puts a stack on the attacker (stage 3).
    /// Upgrade knobs: <paramref name="reflectPercent"/> returns a share of barrier-absorbed damage to
    /// the attacker as cold; <paramref name="healPerTurnPercent"/> heals the bearer each turn end.
    /// </summary>
    public class IceAegisEffect(
        int duration,
        EffectValue amount,
        Func<IEffect>? attackerEffectFactory = null,
        Action? onBarrierBroken = null,
        EffectValue reflectPercent = default,
        EffectValue healPerTurnPercent = default)
        : Effect(id: "Effect_Ice_Aegis", duration, maxStacks: 1)
    {
        private float _granted;
        private bool _brokenHandled;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;

            float before = Target.CurrentBarrier;
            Target.CurrentBarrier += Effective(amount);
            _granted = Target.CurrentBarrier - before; // MaxBarrier may clamp the grant
            Target.CurrentBarrierChanged += OnBarrierChanged;
            if (attackerEffectFactory != null || Effective(reflectPercent) > 0)
                SubscribeUntilRemoved<DamageTakenEvent>(Target.CombatEvents, OnDamageTaken);
        }

        public override void Remove()
        {
            if (Target != null)
            {
                Target.CurrentBarrierChanged -= OnBarrierChanged;
                // The remainder burns. Consumption per source is not tracked: we burn at most the
                // granted amount — barrier from other sources on top of it survives.
                if (!_brokenHandled && _granted > 0)
                    Target.CurrentBarrier = Mathf.Max(0, Target.CurrentBarrier - _granted);
            }

            base.Remove();
        }

        public override void TurnEnd()
        {
            if (Target != null && Effective(healPerTurnPercent) > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * Effective(healPerTurnPercent) });
            base.TurnEnd();
        }

        public override IEffect Copy() =>
            new IceAegisEffect(Duration, amount, attackerEffectFactory, onBarrierBroken, reflectPercent, healPerTurnPercent);

        private void OnBarrierChanged(float value)
        {
            if (value > 0 || _brokenHandled) return;
            _brokenHandled = true;
            onBarrierBroken?.Invoke();
            Remove(); // the aegis is shattered — nothing left to guard or to burn
        }

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (Target == null) return;
            var context = evt.Context;
            if (context.Cause is not (DamageCause.Attack or DamageCause.Ability)) return;
            var attacker = context.Source;
            if (attacker.IsSame(Target.InstanceId) || !attacker.IsAlive) return;

            if (attackerEffectFactory != null)
                // The stack the aegis puts on its attacker is content the AEGIS lays, so it lands as
                // hard as the cast that raised the aegis did: the cast's own claim travels on, one step
                // further from the ability, exactly as it would if the cast had applied it directly.
                // What the aegis grew to under the bearer's knobs stays with the aegis — the stack runs
                // those knobs itself, and for its own kind.
                _ = attackerEffectFactory().Apply(new EffectApplyingContext
                {
                    Caster = Target, Target = attacker, Source = InstanceId, Effectiveness = CastEffectiveness, Trace = Trace
                });

            float reflected = context.AbsorbedByBarrier * Effective(reflectPercent);
            if (reflected <= 0) return;
            var reflection = new DamageContext { Source = Target, Cause = DamageCause.Effect };
            reflection.Add(DamageType.Cold, reflected);
            _ = attacker.TakeDamage(reflection);
        }
    }
}
