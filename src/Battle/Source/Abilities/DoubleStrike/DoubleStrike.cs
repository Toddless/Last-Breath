namespace Battle.Source.Abilities.DoubleStrike
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Components.Decorator;
    using Core.Components.Module;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Two consecutive strikes with individual damage numbers: the first shreds armor on a hit,
    /// the second shreds evasion. L3a: both landing grants a damage buff (factory-injected);
    /// L3b: the first hit restores health, the second — mana.
    /// </summary>
    public class DoubleStrike(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        float secondDamage,
        float secondWeaponDamageScale,
        float secondSpellDamageScale,
        float armorReduce,
        float evadeReduce,
        int debuffDuration,
        int debuffMaxStacks,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Double_Strike", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.SecondDamage] = new Module<Parameters>(() => secondDamage, Parameters.SecondDamage),
                    [Parameters.SecondWeaponScale] = new Module<Parameters>(() => secondWeaponDamageScale, Parameters.SecondWeaponScale),
                    [Parameters.SecondSpellScale] = new Module<Parameters>(() => secondSpellDamageScale, Parameters.SecondSpellScale),
                    [Parameters.DamageMultiplier] = new Module<Parameters>(() => 1f, Parameters.DamageMultiplier),
                    [Parameters.HealthRestore] = new Module<Parameters>(() => 0f, Parameters.HealthRestore),
                    [Parameters.ManaRestore] = new Module<Parameters>(() => 0f, Parameters.ManaRestore)
                });
                return field;
            }
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                AddModuleValues(values, AbilityParameterDecorator);
                return values;
            }
        }

        public float SecondDamage => this[Parameters.SecondDamage];
        public float SecondWeaponScale => this[Parameters.SecondWeaponScale];
        public float SecondSpellScale => this[Parameters.SecondSpellScale];
        public float DamageMultiplier => this[Parameters.DamageMultiplier];
        public float HealthRestore => this[Parameters.HealthRestore];
        public float ManaRestore => this[Parameters.ManaRestore];
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>L3 upgrade point: built when both strikes land, applied to the owner.</summary>
        public Func<IEffect>? BothHitsBuffFactory { get; set; }

        public enum Parameters : byte
        {
            SecondDamage,
            SecondWeaponScale,
            SecondSpellScale,
            DamageMultiplier,
            HealthRestore,
            ManaRestore
        }

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParameterDecorator.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParameterDecorator.RemoveDecorator(id, parameter);
        }

        public override IAbility Copy()
        {
            var copy = new DoubleStrike(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                SecondDamage, SecondWeaponScale, SecondSpellScale, armorReduce, evadeReduce, debuffDuration, debuffMaxStacks, CostType)
            {
                BothHitsBuffFactory = BothHitsBuffFactory
            };
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

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
                        await ApplyDebuff(new ArmorReductionEffect(debuffDuration, debuffMaxStacks, armorReduce), owner, target);
                        RestoreHealth(owner);
                    }
                    else
                    {
                        secondLanded = true;
                        await ApplyDebuff(new Clumsiness(debuffDuration, debuffMaxStacks, evadeReduce), owner, target);
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
            owner.Heal(new HealContext(owner, owner) { Amount = owner.Parameters.MaxHealth * HealthRestore, Cause = HealCause.Direct });
        }

        private void RestoreMana(IFightable owner)
        {
            if (ManaRestore <= 0) return;
            owner.RestoreMana(new ManaRecoveryContext(owner, owner) { Amount = owner.Parameters.MaxMana * ManaRestore });
        }
    }
}
