namespace Battle.Source.Abilities.IncreasingPressure
{
    using Module;
    using Utilities;
    using Core.Enums;
    using Decorators;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Components.Decorator;

    public class IncreasingPressure(
        string[] tags,
        int costValue,
        int cooldown,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int maxAttacks,
        float increaseAttackDamageStep,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Increasing_Pressure", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, upgrades, costType)
    {
        private float this[Parameters parameters] => AbilityParametersModuleManager.GetModule(parameters).GetValue();

        public IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;

                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(
                    new Dictionary<Parameters, IParameterModule<Parameters>>
                    {
                        [Parameters.Attacks] = new Module<Parameters>(() => maxAttacks, Parameters.Attacks),
                        [Parameters.IncreaseAttackDamageStep] = new Module<Parameters>(() => increaseAttackDamageStep, Parameters.IncreaseAttackDamageStep),
                    });
                return field;
            }
        }


        public float Attacks => this[Parameters.Attacks];
        public float IncreaseAttackDamage => this[Parameters.IncreaseAttackDamageStep];
        public IIpExecutionStrategy ExecutionStrategy = new IpDefaultExecutionStrategy([]);

        public enum Parameters : byte
        {
            Attacks,
            IncreaseAttackDamageStep
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> abilityParameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParametersModuleManager.AddDecorator(abilityParameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters pKey)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParametersModuleManager.RemoveDecorator(id, pKey);
        }

        public override IAbility Copy() => new IncreasingPressure(Tags, CostValue, (int)Cooldown, Damage, WeaponDamageScale, SpellDamageScale, maxAttacks, IncreaseAttackDamage,
            Upgrades, CostType);

        protected override async Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field) =>
            await ExecutionStrategy.Execute(this, owner, targets, field);

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, maxAttacks, increaseAttackDamageStep * 100);
    }
}
