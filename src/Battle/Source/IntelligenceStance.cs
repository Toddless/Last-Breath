namespace Battle.Source
{
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;
    using PassiveSkills;

    public class IntelligenceStance(IFightable owner)
        : StanceBase(owner, effect: new StanceActivationEffect([new ResonancePassiveSkill(0.04f, 0.10f)],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Intelligence, 15)]), Stance.Intelligence)
    {
    }
}
