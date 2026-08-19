namespace Battle.Source.Abilities.BerserkFury
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;
    using Godot;

    /// <summary>
    /// Consecutive attacks: the chance to continue the series scales with CURRENT health, and the cast
    /// puts the Fury effect on the caster (burns health per attack) — the series self-balances.
    /// An augment swaps the Fury variant through <see cref="FuryFactory"/>.
    /// </summary>
    public class BerserkFury(AbilityBaseData data) : DamagingAbility(data), IAttackModifierHost
    {
        private const float MinContinueChance = 0.05f;
        private const float MaxContinueChance = 0.80f;

        /// <summary>How long the Fury on the caster holds — the common buff duration, so "+duration"
        /// lengthens the burn along with the series.</summary>
        public int FuryDuration => (int)this[AbilityParameter.Duration];

        public float FuryHealthPercent => this[Parameters.FuryHealthPercent];
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>Augment point: which Fury variant the cast applies (duration, healthPercent) → effect.</summary>
        public Func<int, float, IEffect> FuryFactory { get; set; } =
            (duration, healthPercent) => new FuryEffect(duration, maxStacks: 1, healthPercent);

        public static class Parameters
        {
            /// <summary>Share of current health each attack burns — what the fury COSTS, not effectiveness.</summary>
            public const string FuryHealthPercent = nameof(FuryHealthPercent);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterCriticalParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.AccuracyBonus, 0f);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(Parameters.FuryHealthPercent, 0.05f);
        }

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        public override IAbility Copy() => CopyUpgradesTo(new BerserkFury(Data) { FuryFactory = FuryFactory });

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // Fury goes on first: it burns health on every attack of the series below.
            await FuryFactory(FuryDuration, FuryHealthPercent)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId, Trace = Trace });

            foreach (IFightable target in targets)
            {
                var window = new AttackSeriesWindow(this, owner, field);
                int attackIndex = 0;
                while (owner.CurrentHealth > 1 && target.IsAlive)
                {
                    float additionalDamage = Damage + (owner.Parameters.PhysicalDamage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
                    var context = new AttackContext(owner, target, owner.Parameters.PhysicalDamage, CombatRandom.Attacks!, window.Scheduler)
                    {
                        Index = attackIndex++,
                        SourceAbilityId = Id
                    };
                    context.UseCriticalOf(this);
                    context.UseAccuracyOf(this);
                    context.AddDamage(DamageType.Physical, additionalDamage);
                    AttackModifiers.ApplyAll(context);

                    if (!await window.ResolveAsync(context)) break;

                    // Lower health — lower chance to keep swinging (fury burns health, so the series ends itself).
                    float chance = Mathf.Clamp(owner.CurrentHealth / owner.Parameters.MaxHealth, MinContinueChance, MaxContinueChance);
                    if (!ChanceRoll.Roll(chance, CombatRandom.Rolls)) break;
                }
            }
        }
    }
}
