namespace Battle.Source.Abilities.Armageddon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.Components.Decorator;
    using Core.Entity.Components.Module;
    using Core.Enums;
    using Effects;
    using Godot;
    using HitDelivery;

    /// <summary>
    /// Charged ability: holding the button picks one of three stages, each consuming a share of max
    /// health and dealing its own damage; stage 3 additionally stuns. Direct hits (no attack pipeline).
    /// Stage-1 numbers live in the base damage modules so upgrades can override them; if health runs
    /// short at cast time the stage downgrades to the highest affordable one.
    /// </summary>
    public class Armageddon(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        float secondDamage,
        float secondWeaponScale,
        float secondSpellScale,
        float thirdDamage,
        float thirdWeaponScale,
        float thirdSpellScale,
        float stage1HpCost,
        float stage2HpCost,
        float stage3HpCost,
        int stunDuration,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Armageddon", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType), IChargedAbility
    {
        /// <summary>"+1 damage per every 5 missing health" — the missing-health step of the L3 upgrade.</summary>
        private const float MissingHpStep = 5f;

        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.StunDuration] = new Module<Parameters>(() => stunDuration, Parameters.StunDuration),
                    [Parameters.HpCostMultiplier] = new Module<Parameters>(() => 1f, Parameters.HpCostMultiplier),
                    [Parameters.MissingHpRate] = new Module<Parameters>(() => 0f, Parameters.MissingHpRate)
                });
                return field;
            }
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                AddModuleValues(values, AbilityParametersModuleManager);
                return values;
            }
        }

        public int StunDuration => (int)this[Parameters.StunDuration];
        public float HpCostMultiplier => this[Parameters.HpCostMultiplier];
        public float MissingHpRate => this[Parameters.MissingHpRate];

        public IHitSequenceStrategy HitSequence { get; set; } = new SelectedTargetsHits();

        /// <summary>L2 upgrade point: extra effect stage 3 puts on every hit target (e.g. burning stacks).</summary>
        public Func<IEffect>? Stage3EffectFactory { get; set; }
        public int Stage3EffectStacks { get; set; } = 1;

        public int MaxStage => 3;
        public int PendingStage { get; set; }

        /// <summary>The charge is a commitment: once selection begins there is no backing out.</summary>
        public bool IsCancellable => false;

        public int MaxAffordableStage
        {
            get
            {
                if (Owner == null) return 1;
                for (int stage = MaxStage; stage > 1; stage--)
                    if (HpCost(stage) < Owner.CurrentHealth)
                        return stage;
                return 1;
            }
        }

        public enum Parameters : byte
        {
            StunDuration,
            HpCostMultiplier,
            MissingHpRate
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParametersModuleManager.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParametersModuleManager.RemoveDecorator(id, parameter);
        }

        public override IAbility Copy()
        {
            var copy = new Armageddon(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                secondDamage, secondWeaponScale, secondSpellScale, thirdDamage, thirdWeaponScale, thirdSpellScale,
                stage1HpCost, stage2HpCost, stage3HpCost, StunDuration, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            int stage = Mathf.Clamp(PendingStage == 0 ? 1 : PendingStage, 1, MaxStage);
            PendingStage = 0;
            // "При отсутствии необходимого уровня здоровья активируется последняя достигнутая стадия."
            stage = Mathf.Min(stage, MaxAffordableStage);
            owner.ConsumeResource(Costs.Health, HpCost(stage));

            float abilityDamage = StageDamage(stage, owner);
            foreach (IFightable target in HitSequence.GetHitSequence(owner, targets, field))
            {
                if (!target.IsAlive) continue;
                float total = abilityDamage;
                if (MissingHpRate > 0)
                    total += (target.Parameters.MaxHealth - target.CurrentHealth) / MissingHpStep * MissingHpRate;

                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Physical, total);
                await target.TakeDamage(context);

                if (stage >= MaxStage) await ApplyStageThreeEffects(owner, target, context.TotalDamage);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, IsCritical: false, context.TotalDamage));
            }
        }

        private async Task ApplyStageThreeEffects(IFightable owner, IFightable target, float damageDealt)
        {
            await new StunEffect(StunDuration).Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
            if (Stage3EffectFactory == null) return;
            await Stage3EffectFactory().ApplyStacks(
                new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId, Damage = damageDealt },
                Stage3EffectStacks);
        }

        private float HpCost(int stage) => (Owner?.Parameters.MaxHealth ?? 0) * StageHpPercent(stage) * HpCostMultiplier;

        private float StageHpPercent(int stage) => stage switch
        {
            1 => stage1HpCost,
            2 => stage2HpCost,
            _ => stage3HpCost
        };

        private float StageDamage(int stage, IFightable owner) => stage switch
        {
            1 => Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale),
            2 => secondDamage + (owner.Parameters.Damage * secondWeaponScale) + (owner.Parameters.SpellDamage * secondSpellScale),
            _ => thirdDamage + (owner.Parameters.Damage * thirdWeaponScale) + (owner.Parameters.SpellDamage * thirdSpellScale)
        };
    }
}
