namespace Battle.Source.Abilities.ManaDevour
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;

    public class ManaDevour(
        string[] tags,
        int costValue,
        int cooldown,
        float percentManaToConsume,
        float increaseBonusPerManaConsumed,
        Costs costType = Costs.Mana) :
        Ability(id: "Ability_Mana_Devour",
            tags,
            cooldown,
            costValue,
            costType)
    {
        public float PercentToConsume { get; } = percentManaToConsume;
        public float IncreaseBonusPerManaConsumed { get; } = increaseBonusPerManaConsumed;

        public override IAbility Copy()
        {
            var copy = new ManaDevour(Tags, CostValue, (int)Cooldown, PercentToConsume, IncreaseBonusPerManaConsumed, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

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
