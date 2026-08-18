namespace Battle.Source.Abilities.DoubleStrike
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

    /// <summary>
    /// Two consecutive strikes with individual damage numbers: the first shreds armor on a hit,
    /// the second shreds evasion. L3a: both landing grants a damage buff (factory-injected);
    /// L3b: the first hit restores health, the second — mana.
    /// </summary>
    public class DoubleStrike(AbilityBaseData data) : DamagingAbility(data), IAttackModifierHost
    {
        public int DebuffDuration => (int)this[Parameters.DebuffDuration];
        public int DebuffMaxStacks => (int)this[AbilityParameter.Stacks];
        public float SecondDamage => this[Parameters.SecondDamage];
        public float SecondWeaponScale => this[Parameters.SecondWeaponScale];
        public float SecondSpellScale => this[Parameters.SecondSpellScale];
        public float DamageMultiplier => this[AbilityParameter.DamageMultiplier];
        public float HealthRestore => this[AbilityParameter.HealthRestore];
        public float ManaRestore => this[AbilityParameter.ManaRestore];
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>L3 upgrade point: built when both strikes land, applied to the owner.</summary>
        public Func<IEffect>? BothHitsBuffFactory { get; set; }

        public static class Parameters
        {
            public const string SecondDamage = nameof(SecondDamage);
            public const string SecondWeaponScale = nameof(SecondWeaponScale);
            public const string SecondSpellScale = nameof(SecondSpellScale);
            public const string ArmorReduce = nameof(ArmorReduce);
            public const string EvadeReduce = nameof(EvadeReduce);

            /// <summary>How long the strikes' debuff holds on the TARGET — not the caster-side
            /// <see cref="AbilityParameter.Duration"/>.</summary>
            public const string DebuffDuration = nameof(DebuffDuration);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterCriticalParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.AccuracyBonus, 0f);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(Parameters.SecondDamage, 60f);
            parameters.RegisterDefault(Parameters.SecondWeaponScale, 1f);
            parameters.RegisterDefault(Parameters.SecondSpellScale, 0.25f);
            parameters.RegisterDefault(Parameters.ArmorReduce, 0.15f);
            parameters.RegisterDefault(Parameters.EvadeReduce, 0.15f);
            parameters.RegisterAppliedDuration(Parameters.DebuffDuration, 3);
            parameters.RegisterDefault(AbilityParameter.Stacks, 3);
            parameters.RegisterDefault(AbilityParameter.DamageMultiplier, 1f);
            parameters.RegisterDefault(AbilityParameter.HealthRestore, 0f);
            parameters.RegisterDefault(AbilityParameter.ManaRestore, 0f);
        }

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        public override IAbility Copy() =>
            CopyUpgradesTo(new DoubleStrike(Data) { BothHitsBuffFactory = BothHitsBuffFactory });

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in targets)
            {
                var window = new AttackSeriesWindow(this, owner, field);
                bool firstLanded = false, secondLanded = false;
                for (int strike = 0; strike < 2; strike++)
                {
                    if (!target.IsAlive) break;
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, CombatRandom.Attacks!, window.Scheduler)
                    {
                        Index = strike,
                        TotalCount = 2,
                        SourceAbilityId = Id
                    };
                    context.UseCriticalOf(this);
                    context.UseAccuracyOf(this);
                    context.AddDamage(DamageType.Physical, StrikeDamage(strike, owner));
                    AttackModifiers.ApplyAll(context);

                    if (!await window.ResolveAsync(context)) break;

                    if (context.Result is not AttackResults.Succeed) continue;
                    if (strike == 0)
                    {
                        firstLanded = true;
                        await ApplyDebuff(new ArmorReductionEffect(DebuffDuration, DebuffMaxStacks, this[Parameters.ArmorReduce]), owner, target);
                        await Restore(owner, HealthRestore, manaShare: 0f);
                    }
                    else
                    {
                        secondLanded = true;
                        await ApplyDebuff(new Clumsiness(DebuffDuration, DebuffMaxStacks, this[Parameters.EvadeReduce]), owner, target);
                        await Restore(owner, healthShare: 0f, ManaRestore);
                    }
                }

                if (firstLanded && secondLanded && BothHitsBuffFactory != null)
                    await BothHitsBuffFactory().Apply(Laying(owner));
            }
        }

        private float StrikeDamage(int strike, IFightable owner)
        {
            float abilityDamage = strike == 0
                ? Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale)
                : SecondDamage + (owner.Parameters.Damage * SecondWeaponScale) + (owner.Parameters.SpellDamage * SecondSpellScale);
            return abilityDamage * DamageMultiplier;
        }

        private async Task ApplyDebuff(IEffect debuff, IFightable owner, IFightable target) =>
            await debuff.Apply(Laying(target));

        /// <summary>What a landed strike gives back, laid as an effect like every other number of the
        /// cast — so the cast's effectiveness reaches it without a multiplication written here.</summary>
        private async Task Restore(IFightable owner, float healthShare, float manaShare)
        {
            if (healthShare <= 0 && manaShare <= 0) return;

            await new InstantRestoreEffect(healthShare, manaShare).Apply(Laying(owner));
        }
    }
}
