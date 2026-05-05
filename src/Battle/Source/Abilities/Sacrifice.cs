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
        List<IEffect> effects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast)
        : Ability(id: "Ability_Sacrifice", tags, cooldown, costValue, effects, upgrades, costType, abilityType)
    {
        public float PercentHealthToSacrifice { get; } = percentHealthToSacrifice;

        public override async Task Execute(List<IEntity> targets)
        {
            if (Owner == null) return;

            float sacrificedLife = Owner.CurrentHealth * PercentHealthToSacrifice;
            var modifier = new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, sacrificedLife, Id);
            Owner.CurrentHealth -= (int)sacrificedLife;
            Owner.Modifiers.AddModifier(modifier);
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
            await base.Execute(targets);
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentHealthToSacrifice * 100);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner?.Modifiers.RemoveModifierBySource(Id);
        }
    }
}
