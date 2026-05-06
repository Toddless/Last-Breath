namespace Battle.Source.Abilities
{
    using Utilities;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
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
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast) : Ability(id: "Ability_Mana_Devour", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0,
        upgrades,
        costType, abilityType)
    {
        public float PercentToConsume { get; } = percentManaToConsume;
        public float IncreaseBonusPerManaConsumed { get; } = increaseBonusPerManaConsumed;


        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentToConsume * 100, IncreaseBonusPerManaConsumed * 100);

        private void OnAbilityActivated(AbilityActivationEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivationEvent>(OnAbilityActivated);
            Owner?.Modifiers.RemoveModifierBySource(Id);
        }
    }
}
