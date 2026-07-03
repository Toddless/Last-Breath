namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Module;
    using Source.Decorators;

    public class SeriesOfDamagings(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int minAttacks,
        int maxAttacks,
        float damageMultiplier = 1,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Series_Of_Attacks", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private Dictionary<string, IAttackModifier> _attackModifiers = [];

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.DamageMultiplier] = new Module<Parameters>(() => damageMultiplier, Parameters.DamageMultiplier),
                    [Parameters.MinAttacks] = new Module<Parameters>(() => minAttacks, Parameters.MinAttacks),
                    [Parameters.MaxAttacks] = new Module<Parameters>(() => maxAttacks, Parameters.MaxAttacks)
                });
                field.ModuleChanges += OnModuleChanges;
                return field;
            }
        }

        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();
        public int MinAttacks => (int)this[Parameters.MinAttacks];
        public int MaxAttacks => (int)this[Parameters.MaxAttacks];
        public float DamageMultiplier => this[Parameters.DamageMultiplier];
        public ISoAExecutionStrategy ExecutionStrategy { get; set; } = new SoAsDefaultExecutionStrategy();
        public IReadOnlyList<IAttackModifier> AttackModifiers => _attackModifiers.Values.ToList();

        public enum Parameters : byte
        {
            DamageMultiplier,
            MinAttacks,
            MaxAttacks
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

        public void AddAttackModifier(IAttackModifier modifier) => _attackModifiers.TryAdd(modifier.Id, modifier);
        public void RemoveAttackModifier(string id) => _attackModifiers.Remove(id);

        public override IAbility Copy()
        {
            var copy = new SeriesOfDamagings(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale, MinAttacks, MaxAttacks, DamageMultiplier, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) => await ExecutionStrategy.Execute(this, owner, targets, field);
    }
}
