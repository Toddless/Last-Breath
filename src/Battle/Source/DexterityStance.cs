namespace Battle.Source
{
    using Abilities;
    using Abilities.Effects;
    using Abilities.IncreasingPressure;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using PassiveSkills;

    public class DexterityStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new ChainAttackPassiveSkill()],
            [new Modifier(ModifierValueType.Flat, EntityParameter.Dexterity, 15)]), Stance.Dexterity, [
            new IncreasingPressure([], 50, 4, [], [], []),
            new DarkShroud([], 50, 5, [], [new LifeGivingShadeEffect(300, 3, 3)], [])
        ])
    {
    }
}
