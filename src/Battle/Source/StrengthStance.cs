namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using PassiveSkills;

    public class StrengthStance(IFightable owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new TrappedBeastPassiveSkill(0.05f, 0.05f)],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 15)]), Stance.Strength,
            [
            ]);
}
