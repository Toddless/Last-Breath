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
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgradeWrap<>>> upgrades,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast) : Ability(id: "Ability_Mana_Devour", tags, cooldown, costValue,  effects, casterEffects, upgrades,
        costType, abilityType)
    {
        public float PercentToConsume { get; } = percentManaToConsume;
        public float IncreaseBonusPerManaConsumed { get; } = increaseBonusPerManaConsumed;

        public override async Task Execute(List<IEntity> targets)
        {
            if (Owner == null) return;

            float manaConsumed = Owner.CurrentMana * PercentToConsume;
            float increase = manaConsumed / 100 / IncreaseBonusPerManaConsumed;
            var modifier = new SimpleModifier(EntityParameter.SpellDamage, ModifierValueType.Increase, increase, Id);
            Owner.Modifiers.AddModifier(modifier);
            Owner.CombatEvents.Subscribe<AbilityActivatedEvent>(OnAbilityActivated);
            await base.Execute(targets);
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentToConsume * 100, IncreaseBonusPerManaConsumed * 100);

        private void OnAbilityActivated(AbilityActivatedEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivatedEvent>(OnAbilityActivated);
            Owner?.Modifiers.RemoveModifierBySource(Id);
        }
    }
}
