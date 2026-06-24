namespace Battle.Source
{
    using Core.Enums;
    using PassiveSkills;
    using Core.Modifiers;
    using Core.Interfaces.Entity;

    public class StrengthStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new TrappedBeastPassiveSkill(0.05f, 0.05f)],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 15)]), Stance.Strength,
            [
            ]);
}
