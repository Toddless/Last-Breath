namespace Battle.Source.Abilities.IceAegis
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
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>Cast plan of the Ice Aegis: barrier numbers and the optional stage payloads.</summary>
    public class AegisPlan
    {
        public float BarrierBase { get; set; }
        public float PerIntelligenceScale { get; set; }
        public int Duration { get; set; }
        public Func<IEffect>? AttackerEffectFactory { get; set; }
        public Action? OnBarrierBroken { get; set; }
    }

    /// <summary>
    /// Grants a barrier scaling with intelligence for a few turns; the remainder burns on expiry.
    /// Stage 2 raises the intelligence scale, stage 3 makes attackers catch Clumsiness while the
    /// aegis holds, stage 4 freezes every enemy when the barrier is shattered early — the break
    /// reaction lives inside the barrier EFFECT, the ability only wires it up.
    /// </summary>
    public class IceAegis(
        string[] tags,
        int cooldown,
        int costValue,
        float barrierBase,
        float perIntelligenceScale,
        int duration,
        float stageTwoScaleBonus,
        int clumsinessDuration,
        int clumsinessMaxStacks,
        float clumsinessValue,
        int freezeDuration,
        Costs costType = Costs.Mana)
        : MulticastAbility<AegisPlan>(id: "Ability_Ice_Aegis", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0, costType)
    {
        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.BarrierBase] = new Module<Parameters>(() => barrierBase, Parameters.BarrierBase),
                    [Parameters.PerIntelligenceScale] = new Module<Parameters>(() => perIntelligenceScale, Parameters.PerIntelligenceScale),
                    [Parameters.Duration] = new Module<Parameters>(() => duration, Parameters.Duration)
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

        public float BarrierBase => this[Parameters.BarrierBase];
        public float PerIntelligenceScale => this[Parameters.PerIntelligenceScale];
        public int Duration => (int)this[Parameters.Duration];

        public enum Parameters : byte
        {
            BarrierBase,
            PerIntelligenceScale,
            Duration
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
            var copy = new IceAegis(Tags, (int)Cooldown, CostValue, BarrierBase, PerIntelligenceScale, Duration,
                stageTwoScaleBonus, clumsinessDuration, clumsinessMaxStacks, clumsinessValue, freezeDuration, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override AegisPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                BarrierBase = BarrierBase,
                PerIntelligenceScale = PerIntelligenceScale,
                Duration = Duration
            };

        protected override void ApplyStage(int stage, AegisPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.PerIntelligenceScale += stageTwoScaleBonus;
                    break;
                case 3:
                    plan.AttackerEffectFactory = () => new Clumsiness(clumsinessDuration, clumsinessMaxStacks, clumsinessValue);
                    break;
                case 4:
                    plan.OnBarrierBroken = () => FreezeAllEnemies(owner, field);
                    break;
            }
        }

        protected override async Task ExecutePlan(AegisPlan plan, IFightable owner)
        {
            float intelligence = owner.Parameters.GetValueForParameter(EntityParameter.Intelligence);
            float amount = plan.BarrierBase + (plan.PerIntelligenceScale * intelligence);
            await new IceAegisEffect(plan.Duration, amount, plan.AttackerEffectFactory, plan.OnBarrierBroken)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
        }

        private void FreezeAllEnemies(IFightable owner, IBattleField field)
        {
            foreach (IFightable enemy in field.GetEnemies(owner).Where(e => e.IsAlive))
                _ = new FreezeEffect(freezeDuration)
                    .Apply(new EffectApplyingContext { Caster = owner, Target = enemy, Source = InstanceId });
        }
    }
}
