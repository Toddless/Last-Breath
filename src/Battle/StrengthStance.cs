namespace Battle
{
    using Core.Enums;
    using Core.Modifiers;
    using Source.Abilities;
    using Source.PassiveSkills;
    using Core.Interfaces.Entity;
    using Source.Abilities.Effects;

    public class StrengthStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new TrappedBeastPassiveSkill(0.05f, 0.05f)],
                [new Modifier(ModifierType.Flat, EntityParameter.Strength, 15)]), Stance.Strength,
            [
                new BerserkFury(5, 1, [], 50, 100, [], [new FuryEffect(3, 0.05f)], []),
                new Sacrifice([], 50, 50, 5, 0.5f, [], [], [])
            ]);
}
