namespace Battle.Source.Abilities.BerserkFury
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Effects;
    using Godot;
    using Module;
    using Decorators;

    /// <summary>
    /// Consecutive attacks: the chance to continue the series scales with CURRENT health, and the cast
    /// puts the Fury effect on the caster (burns health per attack) — the series self-balances.
    /// L3 swaps the Fury variant through <see cref="FuryFactory"/>.
    /// </summary>
    public class BerserkFury(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int furyDuration,
        float furyHealthPercent,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Berserk_Fury", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private const float MinContinueChance = 0.05f;
        private const float MaxContinueChance = 0.80f;

        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.FuryDuration] = new Module<Parameters>(() => furyDuration, Parameters.FuryDuration),
                    [Parameters.FuryHealthPercent] = new Module<Parameters>(() => furyHealthPercent, Parameters.FuryHealthPercent)
                });
                return field;
            }
        }

        public int FuryDuration => (int)this[Parameters.FuryDuration];
        public float FuryHealthPercent => this[Parameters.FuryHealthPercent];
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>L3 upgrade point: which Fury variant the cast applies (duration, healthPercent) → effect.</summary>
        public Func<int, float, IEffect> FuryFactory { get; set; } =
            (duration, healthPercent) => new FuryEffect(duration, maxStacks: 1, healthPercent);

        public enum Parameters : byte
        {
            FuryDuration,
            FuryHealthPercent
        }

        public void AddAttackModifier(Core.Interfaces.IAttackModifier modifier) => AttackModifiers.Add(modifier);
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
            var copy = new BerserkFury(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                FuryDuration, FuryHealthPercent, CostType) { FuryFactory = FuryFactory };
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // Fury goes on first: it burns health on every attack of the series below.
            await FuryFactory(FuryDuration, FuryHealthPercent)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });

            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            var cts = new CancellationTokenSource();

            foreach (IFightable target in targets)
            {
                var scheduler = new AttackContextScheduler();
                int attackIndex = 0;
                while (owner.CurrentHealth > 1 && target.IsAlive)
                {
                    float additionalDamage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, rnd, scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        AdditionalDamage = additionalDamage,
                        Index = attackIndex++
                    };
                    AttackModifiers.ApplyAll(context);

                    if (!context.Schedule()) break;
                    await foreach (var processed in scheduler.RunQueue(cts.Token))
                    {
                        if (processed.Attacker.InstanceId != owner.InstanceId) continue;
                        await ApplyImpactRiders(processed.ToImpact(field));
                    }

                    // Lower health — lower chance to keep swinging (fury burns health, so the series ends itself).
                    float chance = Mathf.Clamp(owner.CurrentHealth / owner.Parameters.MaxHealth, MinContinueChance, MaxContinueChance);
                    if (rnd.Randf() > chance) break;
                }
            }
        }
    }
}
