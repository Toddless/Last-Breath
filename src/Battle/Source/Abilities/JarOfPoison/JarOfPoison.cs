namespace Battle.Source.Abilities.JarOfPoison
{
    using Module;
    using Core.Enums;
    using Decorators;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Components.Decorator;

    public class JarOfPoison(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        int poisonDuration,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.Target)
        : Ability(id, tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, upgrades, costType, abilityType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.PoisonDuration] = new Module<Parameters>(() => poisonDuration, Parameters.PoisonDuration)
                });
                return field;
            }
        }

        public int PoisonDuration => (int)this[Parameters.PoisonDuration];
        public IJoPExecutionStrategy ExecutionStrategy { get; set; } = new JoPDefaultExecutionStrategy();

        public enum Parameters
        {
            PoisonDuration
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

        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field) =>
            ExecutionStrategy.Execute(this, owner, targets, field);
    }
}
