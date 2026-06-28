namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;
    using Utilities;

    public class Sacrifice(
        string[] tags,
        int costValue,
        int cooldown,
        float percentHealthToSacrifice,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Sacrifice", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0, costType)
    {
        public float PercentHealthToSacrifice { get; } = percentHealthToSacrifice;

        public override IAbility Copy()
        {
            var copy = new Sacrifice(Tags, CostValue, (int)Cooldown, PercentHealthToSacrifice, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentHealthToSacrifice * 100);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner?.ParameterModifiers.RemoveModifierBySource(Id);
        }
    }
}
