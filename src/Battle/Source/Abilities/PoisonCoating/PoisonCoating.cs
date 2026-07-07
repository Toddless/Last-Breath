namespace Battle.Source.Abilities.PoisonCoating
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
    /// Self-cast buff. For <see cref="CoatingDuration"/> turns, each of the caster's attacks
    /// applies a poison stack to the target.
    /// </summary>
    public class PoisonCoating(
        string[] tags,
        int cooldown,
        int costValue,
        int coatingDuration,
        int poisonDuration,
        float poisonMultiplier,
        Costs costType = Costs.Mana)
        : Ability(
            id: "Ability_Poison_Coating",
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
                    [Parameters.CoatingDuration] = new Module<Parameters>(() => coatingDuration, Parameters.CoatingDuration),
                    [Parameters.PoisonDuration] = new Module<Parameters>(() => poisonDuration, Parameters.PoisonDuration),
                    [Parameters.PoisonMultiplier] = new Module<Parameters>(() => poisonMultiplier, Parameters.PoisonMultiplier)
                });
                return field;
            }
        }

        public int CoatingDuration => (int)AbilityParameterDecorator.GetModule(Parameters.CoatingDuration).GetValue();
        public int PoisonDuration => (int)AbilityParameterDecorator.GetModule(Parameters.PoisonDuration).GetValue();
        public float PoisonDamagePercent => AbilityParameterDecorator.GetModule(Parameters.PoisonMultiplier).GetValue();

        public enum Parameters : byte
        {
            CoatingDuration,
            PoisonDuration,
            PoisonMultiplier
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
            var copy = new PoisonCoating(Tags, cooldown, CostValue, CoatingDuration, PoisonDuration, PoisonDamagePercent, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // Apply the coating buff to the caster; PoisonCoatingEffect handles attack interception
            var coatingBuff = new PoisonCoatingEffect(
                duration: CoatingDuration,
                maxStacks: 1,
                poisonDuration: PoisonDuration,
                poisonDamagePercent: PoisonDamagePercent);

            coatingBuff.Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });

            return Task.CompletedTask;
        }
    }
}
