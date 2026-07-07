namespace Battle.Source.Abilities.DarkShroud
{
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
    using Decorators;
    using Effects;
    using Godot;

    /// <summary>
    /// Self-cast defensive ability. Applies LightStep evasion stacks and percentage health regeneration.
    /// </summary>
    public class DarkShroud(
        string[] tags,
        int cooldown,
        int costValue,
        int stacks,
        float healthRegen,
        float lightStepValue,
        float buffDuration,
        float buffEffectiveness = 1f,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Dark_Shroud",
            tags,
            cooldown,
            costValue,
            costType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.Duration] = new Module<Parameters>(() => buffDuration, Parameters.Duration),
                    [Parameters.Effectiveness] = new Module<Parameters>(() => buffEffectiveness, Parameters.Effectiveness),
                    [Parameters.Stacks] = new Module<Parameters>(() => stacks, Parameters.Stacks),
                    [Parameters.HealthRegen] = new Module<Parameters>(() => healthRegen, Parameters.HealthRegen),
                    [Parameters.LightStepValue] = new Module<Parameters>(() => lightStepValue, Parameters.LightStepValue),
                });
                return field;
            }
        }

        public float Duration => this[Parameters.Duration];
        public float Effectiveness => this[Parameters.Effectiveness];
        public float Stacks => this[Parameters.Stacks];
        public float LightStepValue => this[Parameters.LightStepValue];
        public float HealthRegen => this[Parameters.HealthRegen];

        public enum Parameters
        {
            Effectiveness,
            Duration,
            Stacks,
            HealthRegen,
            LightStepValue
        }

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
            var copy = new DarkShroud(Tags, (int)Cooldown, CostValue, stacks, healthRegen, lightStepValue, buffDuration, buffEffectiveness, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId };
            await new LightStep(Mathf.RoundToInt(Duration), Mathf.RoundToInt(Stacks), LightStepValue * Effectiveness)
                .ApplyStacks(context, Mathf.RoundToInt(Stacks));
            await new HealthRegenerationEffect(HealthRegen * Effectiveness, Mathf.RoundToInt(Duration), 1).Apply(context);
        }
    }
}
