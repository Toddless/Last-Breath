namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Self-cast defensive ability. Applies LightStep evasion stacks and percentage health regeneration.
    /// Base: 4 LightStep stacks, 5% max health regen per turn for 5 turns.
    /// </summary>
    public class DarkShroud(
        string[] tags,
        int cooldown,
        int costValue,
        List<IEffect> effects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast)
        : Ability(
            id: "Ability_Dark_Shroud",
            tags,
            cooldown,
            costValue,
            damage: 0,
            weaponDamageScale: 0,
            spellDamageScale: 0,
            effects,
            upgrades,
            costType,
            abilityType)
    {
        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner)
        {
            var context = new EffectApplyingContext
            {
                Caster = owner,
                Target = owner,
                Source = InstanceId,
                Damage = 0
            };
            ApplyTargetEffects(context);
            return Task.CompletedTask;
        }
    }
}
