namespace Battle.Source.Abilities.IncreasingPressure
{
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
    using Decorators;
    using Module;
    using Utilities;

    public class IncreasingPressure(
        string[] tags,
        int costValue,
        int cooldown,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int maxAttacks,
        float increaseAttackDamageStep,
        Costs costType = Costs.Mana)
        : AttackAbility(id: "Ability_Increasing_Pressure", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameters] => AbilityParametersModuleManager.GetModule(parameters).GetValue();
        private Dictionary<string, IAttackModifier> _attackModifiers = [];

        public IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;

                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(
                    new Dictionary<Parameters, IParameterModule<Parameters>>
                    {
                        [Parameters.Attacks] = new Module<Parameters>(() => maxAttacks, Parameters.Attacks),
                        [Parameters.AttackDamageStepMultiplier] = new Module<Parameters>(() => increaseAttackDamageStep, Parameters.AttackDamageStepMultiplier),
                    });
                return field;
            }
        }

        public IReadOnlyList<IAttackModifier> AttackModifiers => _attackModifiers.Values.ToList();
        public float Attacks => this[Parameters.Attacks];
        public float AttackDamageMultiplier => this[Parameters.AttackDamageStepMultiplier];
        public IIpExecutionStrategy ExecutionStrategy = new IpDefaultExecutionStrategy();

        public enum Parameters : byte
        {
            Attacks,
            AttackDamageStepMultiplier
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

        public override IAbility Copy()
        {
            var copy = new IncreasingPressure(Tags, CostValue, (int)Cooldown, Damage, WeaponDamageScale, SpellDamageScale,
                maxAttacks, AttackDamageMultiplier, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        public void AddAttackModifier(IAttackModifier modifier) => _attackModifiers.TryAdd(modifier.Id, modifier);
        public void RemoveAttackModifier(string id) => _attackModifiers.Remove(id);

        protected override async Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field) =>
            await ExecutionStrategy.Execute(this, owner, targets, field);

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, maxAttacks, increaseAttackDamageStep * 100);
    }
}
