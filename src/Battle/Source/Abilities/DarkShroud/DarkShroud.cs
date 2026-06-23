namespace Battle.Source.Abilities.DarkShroud
{
    using Module;
    using Core.Enums;
    using Decorators;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Components.Module;

    /// <summary>
    /// Self-cast defensive ability. Applies LightStep evasion stacks and percentage health regeneration.
    /// Base: 4 LightStep stacks, 5% max health regen per turn for 5 turns.
    /// </summary>
    public class DarkShroud(
        string[] tags,
        int cooldown,
        int costValue,
        float buffDuration,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        float buffEffectiveness = 1f,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Dark_Shroud",
            tags,
            cooldown,
            costValue,
            damage: 0,
            weaponDamageScale: 0,
            spellDamageScale: 0,
            upgrades,
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
                });
                return field;
            }
        }

        public IDarkShroudExecutionStrategy ExecutionStrategy { get; set; } = new DefaultExecutionStrategy();
        public float BuffDuration => this[Parameters.Duration];
        public float BuffEffectiveness => this[Parameters.Effectiveness];


        public enum Parameters
        {
            Effectiveness,
            Duration,
        }

        public override IAbility Copy() => new DarkShroud(Tags, (int)Cooldown, CostValue, BuffEffectiveness, Upgrades, BuffDuration, CostType);

        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field) => ExecutionStrategy.Execute(targets, owner, field);
    }
}
