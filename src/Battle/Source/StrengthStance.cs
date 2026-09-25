namespace Battle.Source
{
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;
    using PassiveSkills;

    public class StrengthStance(IFightable owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new TrappedBeastPassiveSkill(0.02f, 0.03f)],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 15)]), Stance.Strength);
}
