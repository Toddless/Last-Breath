namespace Battle.Source.Abilities
{
    using Utilities;
    using Core.Enums;
    using Core.Modifiers;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class Sacrifice(
        string[] tags,
        int costValue,
        int cooldown,
        float percentHealthToSacrifice,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast)
        : Ability(id: "Ability_Sacrifice", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0, upgrades, costType, abilityType)
    {
        public float PercentHealthToSacrifice { get; } = percentHealthToSacrifice;

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentHealthToSacrifice * 100);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner?.Modifiers.RemoveModifierBySource(Id);
        }
    }
}
