namespace Battle.Source.Abilities.AresBlessing
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
    /// Self-buff: raises max health and health recovery for a few turns (one composite effect).
    /// L3 upgrades add extra cast effects (incoming damage reduction / turn-end heal / damage buff)
    /// through activation riders with deferred factories, so they follow the current duration.
    /// </summary>
    public class AresBlessing(
        string[] tags,
        int cooldown,
        int costValue,
        int duration,
        float healthBonus,
        float recoveryBonus,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Ares_Blessing", tags, cooldown, costValue, costType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.Duration] = new Module<Parameters>(() => duration, Parameters.Duration),
                    [Parameters.HealthBonus] = new Module<Parameters>(() => healthBonus, Parameters.HealthBonus),
                    [Parameters.RecoveryBonus] = new Module<Parameters>(() => recoveryBonus, Parameters.RecoveryBonus)
                });
                return field;
            }
        }

        public int Duration => (int)this[Parameters.Duration];
        public float HealthBonus => this[Parameters.HealthBonus];
        public float RecoveryBonus => this[Parameters.RecoveryBonus];

        public enum Parameters : byte
        {
            Duration,
            HealthBonus,
            RecoveryBonus
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
            var copy = new AresBlessing(Tags, (int)Cooldown, CostValue, Duration, HealthBonus, RecoveryBonus, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await new AresBlessingEffect(Duration, HealthBonus, RecoveryBonus)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
    }
}
