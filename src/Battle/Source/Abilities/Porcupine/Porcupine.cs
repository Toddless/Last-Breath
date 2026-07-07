namespace Battle.Source.Abilities.Porcupine
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

    /// <summary>
    /// Self-buff: for a few turns every enemy hit is answered with pure damage — a share of the taken
    /// damage plus a share of the bearer's armor. The activatable sibling of PorcupinePassiveSkill.
    /// </summary>
    public class Porcupine(
        string[] tags,
        int cooldown,
        int costValue,
        int duration,
        float damageReturn,
        float armorReturn,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Porcupine", tags, cooldown, costValue, costType)
    {
        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.Duration] = new Module<Parameters>(() => duration, Parameters.Duration),
                    [Parameters.DamageReturn] = new Module<Parameters>(() => damageReturn, Parameters.DamageReturn),
                    [Parameters.ArmorReturn] = new Module<Parameters>(() => armorReturn, Parameters.ArmorReturn),
                    [Parameters.HealOnHit] = new Module<Parameters>(() => 0f, Parameters.HealOnHit),
                    [Parameters.CooldownReduceChance] = new Module<Parameters>(() => 0f, Parameters.CooldownReduceChance)
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

        public int Duration => (int)this[Parameters.Duration];
        public float DamageReturn => this[Parameters.DamageReturn];
        public float ArmorReturn => this[Parameters.ArmorReturn];
        public float HealOnHit => this[Parameters.HealOnHit];
        public float CooldownReduceChance => this[Parameters.CooldownReduceChance];

        public enum Parameters : byte
        {
            Duration,
            DamageReturn,
            ArmorReturn,
            HealOnHit,
            CooldownReduceChance
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
            var copy = new Porcupine(Tags, (int)Cooldown, CostValue, Duration, DamageReturn, ArmorReturn, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await new PorcupineBuffEffect(this, Duration, DamageReturn, ArmorReturn, HealOnHit, CooldownReduceChance)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
    }
}
