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
    public class DoubleStrike(AbilityBaseData data) : DamagingAbility(data)
    {
        public int DebuffDuration => (int)this[Parameters.DebuffDuration];
        public int DebuffMaxStacks => (int)this[Parameters.DebuffMaxStacks];
        public float SecondDamage => this[Parameters.SecondDamage];
        public float SecondWeaponScale => this[Parameters.SecondWeaponScale];
        public float SecondSpellScale => this[Parameters.SecondSpellScale];
        public float DamageMultiplier => this[Parameters.DamageMultiplier];
        public float HealthRestore => this[Parameters.HealthRestore];
        public float ManaRestore => this[Parameters.ManaRestore];
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
            public const string DebuffDuration = nameof(DebuffDuration);
            public const string DebuffMaxStacks = nameof(DebuffMaxStacks);
            public const string DamageMultiplier = nameof(DamageMultiplier);
            public const string HealthRestore = nameof(HealthRestore);
            public const string ManaRestore = nameof(ManaRestore);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.SecondDamage, 60f);
            parameters.RegisterDefault(Parameters.SecondWeaponScale, 1f);
            parameters.RegisterDefault(Parameters.SecondSpellScale, 0.25f);
            parameters.RegisterDefault(Parameters.ArmorReduce, 0.15f);
            parameters.RegisterDefault(Parameters.EvadeReduce, 0.15f);
            parameters.RegisterDefault(Parameters.DebuffDuration, 3);
            parameters.RegisterDefault(Parameters.DebuffMaxStacks, 3);
            parameters.RegisterDefault(Parameters.DamageMultiplier, 1f);
            parameters.RegisterDefault(Parameters.HealthRestore, 0f);
            parameters.RegisterDefault(Parameters.ManaRestore, 0f);
        }

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        public override IAbility Copy() =>
            CopyUpgradesTo(new DoubleStrike(Data) { BothHitsBuffFactory = BothHitsBuffFactory });

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var rnd = new Godot.RandomNumberGenerator();
            rnd.Randomize();

            foreach (IFightable target in targets)
            {
                var window = new AttackSeriesWindow(this, owner, field);
                bool firstLanded = false, secondLanded = false;
                for (int strike = 0; strike < 2; strike++)
                {
                    if (!target.IsAlive) break;
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, rnd, window.Scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        AdditionalDamage = StrikeDamage(strike, owner),
                        Index = strike,
                        TotalCount = 2,
                        SourceAbilityId = Id
                    };
                    AttackModifiers.ApplyAll(context);

                    if (!await window.ResolveAsync(context)) break;

                    if (context.Result is not AttackResults.Succeed) continue;
                    if (strike == 0)
                    {
                        firstLanded = true;
                        await ApplyDebuff(new ArmorReductionEffect(DebuffDuration, DebuffMaxStacks, this[Parameters.ArmorReduce]), owner, target);
                        RestoreHealth(owner);
                    }
                    else
                    {
                        secondLanded = true;
                        await ApplyDebuff(new Clumsiness(DebuffDuration, DebuffMaxStacks, this[Parameters.EvadeReduce]), owner, target);
                        RestoreMana(owner);
                    }
                }

                if (firstLanded && secondLanded && BothHitsBuffFactory != null)
                    await BothHitsBuffFactory().Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
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
            await debuff.Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });

        private void RestoreHealth(IFightable owner)
        {
            if (HealthRestore <= 0) return;
            owner.Heal(new HealContext(owner, owner) { Amount = owner.Parameters.MaxHealth * HealthRestore, Cause = RecoveryCause.Direct });
        }

        private void RestoreMana(IFightable owner)
        {
            if (ManaRestore <= 0) return;
            owner.RestoreMana(new ManaRecoveryContext(owner, owner) { Amount = owner.Parameters.MaxMana * ManaRestore });
        }
    }
}
