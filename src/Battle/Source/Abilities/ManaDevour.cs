namespace Battle.Source.Abilities
{
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class ManaDevour(
        string[] tags,
        int costValue,
        int cooldown,
        float percentManaToConsume,
        float increaseBonusPerManaConsumed,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana) :
        Ability(id: "Ability_Mana_Devour",
            tags,
            cooldown,
            costValue,
            damage: 0,
            weaponDamageScale: 0,
            spellDamageScale: 0,
            upgrades,
            costType)
    {
        public float PercentToConsume { get; } = percentManaToConsume;
        public float IncreaseBonusPerManaConsumed { get; } = increaseBonusPerManaConsumed;

        public override IAbility Copy() => new ManaDevour(Tags, CostValue, (int)Cooldown, PercentToConsume, IncreaseBonusPerManaConsumed, Upgrades, CostType);

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentToConsume * 100, IncreaseBonusPerManaConsumed * 100);

        private void OnAbilityActivated(AbilityActivationEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivationEvent>(OnAbilityActivated);
            Owner?.ParameterModifiers.RemoveModifierBySource(Id);
        }
    }
}
