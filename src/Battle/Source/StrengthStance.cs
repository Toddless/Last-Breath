namespace Battle.Source
{
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.Effects;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using PassiveSkills;

    public class StrengthStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new TrappedBeastPassiveSkill(0.05f, 0.05f)],
                [new Modifier(ModifierType.Flat, EntityParameter.Strength, 15)]), Stance.Strength,
            [
                new BerserkFury([], 5, 100, 1, [], [new FuryEffect(3, 1, 0.05f)], [], null),
                new Sacrifice([], 50, 5, 0.5f, [], [], [])
            ]);
}
