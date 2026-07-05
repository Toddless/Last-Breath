namespace Battle.Source.Abilities.Sacrifice
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
    using Effects;
    using Module;
    using Decorators;

    /// <summary>
    /// Sacrifices a share of CURRENT health; the next activated abilities (charges) deal extra PURE
    /// damage — every 100 health lost gives <c>ratePerHundred</c> of the cast's damage as the bonus.
    /// Cross-stance by design: the charge is an entity effect.
    /// </summary>
    public class Sacrifice(
        string[] tags,
        int cooldown,
        int costValue,
        float sacrificePercent,
        float ratePerHundred,
        int charges,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Sacrifice", tags, cooldown, costValue, costType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.SacrificePercent] = new Module<Parameters>(() => sacrificePercent, Parameters.SacrificePercent),
                    [Parameters.RatePerHundred] = new Module<Parameters>(() => ratePerHundred, Parameters.RatePerHundred),
                    [Parameters.Charges] = new Module<Parameters>(() => charges, Parameters.Charges),
                    [Parameters.HealPercent] = new Module<Parameters>(() => 0f, Parameters.HealPercent)
                });
                return field;
            }
        }

        public float SacrificePercent => this[Parameters.SacrificePercent];
        public float RatePerHundred => this[Parameters.RatePerHundred];
        public int Charges => (int)this[Parameters.Charges];
        public float HealPercent => this[Parameters.HealPercent];

        public enum Parameters : byte
        {
            SacrificePercent,
            RatePerHundred,
            Charges,
            HealPercent
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
            var copy = new Sacrifice(Tags, (int)Cooldown, CostValue, SacrificePercent, RatePerHundred, Charges, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            float sacrificed = owner.CurrentHealth * SacrificePercent;
            owner.ConsumeResource(Costs.Health, sacrificed);

            float bonus = sacrificed / 100f * RatePerHundred;
            await new SacrificeChargeEffect(Id, Charges, bonus, HealPercent)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
        }
    }
}
