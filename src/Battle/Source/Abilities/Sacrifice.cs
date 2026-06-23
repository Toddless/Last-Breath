namespace Battle.Source.Abilities
{
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class Sacrifice(
        string[] tags,
        int costValue,
        int cooldown,
        float percentHealthToSacrifice,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Sacrifice", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0, upgrades, costType)
    {
        public float PercentHealthToSacrifice { get; } = percentHealthToSacrifice;

        public override IAbility Copy() => new Sacrifice(Tags, CostValue, (int)Cooldown, PercentHealthToSacrifice, Upgrades, CostType);

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentHealthToSacrifice * 100);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner?.ParameterModifiers.RemoveModifierBySource(Id);
        }
    }
}
