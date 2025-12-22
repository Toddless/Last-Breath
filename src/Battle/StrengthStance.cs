namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using Source.PassiveSkills;

    public class StrengthStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect(
                [new TrappedBeastPassiveSkill( 0.05f, 0.05f)],
                [new Modifier(ModifierType.Flat, EntityParameter.Strength, 15)]),
            Stance.Strength);
}
