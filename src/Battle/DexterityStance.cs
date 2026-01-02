namespace Battle
{
    using Core.Enums;
    using Core.Modifiers;
    using Source.Abilities;
    using Source.PassiveSkills;
    using Core.Interfaces.Entity;
    using Source.Abilities.Effects;

    public class DexterityStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new ChainAttackPassiveSkill()],
            [new Modifier(ModifierType.Flat, EntityParameter.Dexterity, 15)]), Stance.Dexterity, [
            new IncreasingPressure([], 50, 4, [], [], []),
            new DarkShroud([], 50, 5, [], [new LifeGivingShadeEffect(300, 3, 3)], [])
        ])
    {
    }
}
