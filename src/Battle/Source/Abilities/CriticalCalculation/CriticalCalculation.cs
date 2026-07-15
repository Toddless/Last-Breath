namespace Battle.Source.Abilities.CriticalCalculation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.Components.Decorator;
    using Core.Entity.Components.Module;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Self-cast ability. Applies N stacks of CritCalculationBuff.
    /// Each stack extends its own duration by 1 turn on a critical hit.
    /// </summary>
    public class CriticalCalculation(
        string[] tags,
        int cooldown,
        int costValue,
        int buffStacks,
        int buffDuration,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Critical_Calculation",
            tags,
            cooldown,
            costValue,
            costType)
    {
        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.Stacks] = new Module<Parameters>(() => buffStacks, Parameters.Stacks),
                    [Parameters.Duration] = new Module<Parameters>(() => buffDuration, Parameters.Duration)
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


        public int BuffStacks => (int)this[Parameters.Stacks];
        public int BuffDuration => (int)this[Parameters.Duration];

        /// <summary>
        /// The buff the ability stacks on cast, built from the given duration. Default is the crit-chance
        /// buff; the L3 "replace" upgrade swaps it for an additional-attack-chance buff.
        /// ExecuteInternal applies <see cref="BuffStacks"/> stacks of whatever this returns.
        /// </summary>
        public Func<int, int, IEffect> PrimaryBuffFactory { get; set; } =
            (duration, maxStacks) => new CritCalculationBuff(duration, maxStacks, value: 0.15f);

        public enum Parameters : byte
        {
            Stacks,
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
            var copy = new CriticalCalculation(Tags, (int)Cooldown, CostValue, BuffStacks, BuffDuration, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId };
            int stacks = BuffStacks;
            await PrimaryBuffFactory(BuffDuration, stacks).ApplyStacks(context, stacks);
        }
    }
}
