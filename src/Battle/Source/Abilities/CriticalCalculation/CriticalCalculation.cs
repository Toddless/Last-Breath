namespace Battle.Source.Abilities.CriticalCalculation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Effects;
    using Module;
    using Decorators;

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
        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
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

        public int BuffStacks => (int)AbilityParameterDecorator.GetModule(Parameters.Stacks).GetValue();
        public int BuffDuration => (int)AbilityParameterDecorator.GetModule(Parameters.Duration).GetValue();

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
