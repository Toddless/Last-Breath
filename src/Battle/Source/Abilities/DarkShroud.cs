namespace Battle.Source.Abilities
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public class DarkShroud(
        string[] tags,
        int cooldown,
        int costValue,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.SelfCast)
        : Ability(id: "Ability_Dark_Shroud", tags, cooldown, costValue, maxTargets: 1, effects, casterEffects, upgrades, mastery, costType, abilityType)
    {
    }
}
