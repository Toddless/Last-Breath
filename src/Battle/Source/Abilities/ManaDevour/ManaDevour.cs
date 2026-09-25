namespace Battle.Source.Abilities.ManaDevour
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Events;

    public class ManaDevour(AbilityBaseData data) : Ability(data)
    {
        public static class Parameters
        {
            public const string PercentManaToConsume = nameof(PercentManaToConsume);
            public const string IncreaseBonusPerManaConsumed = nameof(IncreaseBonusPerManaConsumed);
        }

        public float PercentToConsume => this[Parameters.PercentManaToConsume];
        public float IncreaseBonusPerManaConsumed => this[Parameters.IncreaseBonusPerManaConsumed];

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.PercentManaToConsume, 0f);
            parameters.RegisterDefault(Parameters.IncreaseBonusPerManaConsumed, 0f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new ManaDevour(Data));

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) => throw new System.NotImplementedException();

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values["ConsumePercent"] = PercentToConsume * 100f;
                values["SpellDamagePerMana"] = IncreaseBonusPerManaConsumed * 100f;
                return values;
            }
        }

        private void OnAbilityActivated(AbilityActivationEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivationEvent>(OnAbilityActivated);
            Owner?.ParameterModifiers.RemoveModifierBySource(Id);
        }
    }
}
