namespace Battle.Source.Abilities.CriticalCalculation
{
    using Decorators;
    using Module;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;

    /// <summary>
    /// Self-cast ability. Applies N stacks of CritCalculationBuff.
    /// Each stack extends its own duration by 1 turn on a critical hit.
    /// Cost: 100 mana. Cooldown: 5 turns.
    /// </summary>
    public class CriticalCalculation(
        string[] tags,
        int cooldown,
        int costValue,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        int buffStacks,
        int buffDuration,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Critical_Calculation",
            tags,
            cooldown,
            costValue,
            damage: 0,
            weaponDamageScale: 0,
            spellDamageScale: 0,
            upgrades,
            costType)
    {
        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.BuffStacks] = new Module<Parameters>(() => buffStacks, Parameters.BuffStacks),
                    [Parameters.BuffDuration] = new Module<Parameters>(() => buffDuration, Parameters.BuffDuration)
                });
                return field;
            }
        }

        public int BuffStacks => (int)AbilityParameterDecorator.GetModule(Parameters.BuffStacks).GetValue();
        public int BuffDuration => (int)AbilityParameterDecorator.GetModule(Parameters.BuffDuration).GetValue();

        public enum Parameters : byte
        {
            BuffStacks,
            BuffDuration
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

        public override IAbility Copy() => new CriticalCalculation(Tags, (int)Cooldown, CostValue, Upgrades, BuffStacks, BuffDuration, CostType);

        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId, Damage = 0 };

            return Task.CompletedTask;
        }
    }
}
