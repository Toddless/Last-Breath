namespace Battle.Source.Abilities.BerserkFury
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
    /// L3 swaps the Fury variant through <see cref="FuryFactory"/>.
    /// </summary>
    public class BerserkFury(AbilityBaseData data) : DamagingAbility(data)
    {
        private const float MinContinueChance = 0.05f;
        private const float MaxContinueChance = 0.80f;

        /// <summary>
        /// How long the Fury on the caster holds — the book's buff duration, so "your buff lasts longer"
        /// works here and lengthens the burn along with the series. That is the deal the ability offers
        /// and the player takes: a longer fury is more attacks AND more health spent on them.
        /// </summary>
        public int FuryDuration => (int)this[AbilityParameter.Duration];

        public float FuryHealthPercent => this[Parameters.FuryHealthPercent];
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>L3 upgrade point: which Fury variant the cast applies (duration, healthPercent) → effect.</summary>
        public Func<int, float, IEffect> FuryFactory { get; set; } =
            (duration, healthPercent) => new FuryEffect(duration, maxStacks: 1, healthPercent);

        public static class Parameters
        {
            /// <summary>Share of current health each attack of the series burns. The ability's own
            /// bargain and not the book's effectiveness: it is what the fury COSTS, and a record
            /// offering "your buff lands harder" would be offering to raise the price.</summary>
            public const string FuryHealthPercent = nameof(FuryHealthPercent);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
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
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });

            foreach (IFightable target in targets)
            {
                var window = new AttackSeriesWindow(this, owner, field);
                int attackIndex = 0;
                while (owner.CurrentHealth > 1 && target.IsAlive)
                {
                    float additionalDamage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, CombatRandom.Attacks!, window.Scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        Index = attackIndex++,
                        SourceAbilityId = Id
                    };
                    context.AddDamage(DamageType.Physical, additionalDamage);
                    AttackModifiers.ApplyAll(context);

                    if (!await window.ResolveAsync(context)) break;

                    // Lower health — lower chance to keep swinging (fury burns health, so the series ends itself).
                    float chance = Mathf.Clamp(owner.CurrentHealth / owner.Parameters.MaxHealth, MinContinueChance, MaxContinueChance);
                    if (CombatRandom.Rolls.RandFloat() > chance) break;
                }
            }
        }
    }
}
