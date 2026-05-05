namespace Battle.Source
{
    using Abilities;
    using Abilities.Effects;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using PassiveSkills;

    public class StrengthStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new TrappedBeastPassiveSkill(0.05f, 0.05f)],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 15)]), Stance.Strength,
            [
                new BerserkFury([], 5, 100,  [], [new FuryEffect(3, 1, 0.05f)], []),
                new Sacrifice([], 50, 5, 0.5f, [], [])
            ]);
}
