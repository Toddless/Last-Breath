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
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast)
        : Ability(id: "Ability_Sacrifice", tags, cooldown, costValue, maxTargets: 1, effects, casterEffects, upgrades, mastery, costType, abilityType)
    {
        public float PercentHealthToSacrifice { get; } = percentHealthToSacrifice;

        public override async Task Activate(List<IEntity> targets)
        {
            if (Owner == null) return;

            float sacrificedLife = Owner.CurrentHealth * PercentHealthToSacrifice;
            var modifier = new ModifierInstance(EntityParameter.Damage, ModifierType.Flat, sacrificedLife, Id);
            Owner.CurrentHealth -= (int)sacrificedLife;
            Owner.Modifiers.AddPermanentModifier(modifier);
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
            await base.Activate(targets);
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, PercentHealthToSacrifice * 100);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner?.Modifiers.RemovePermanentModifierBySource(Id);
        }
    }
}
