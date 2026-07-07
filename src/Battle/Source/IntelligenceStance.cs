namespace Battle.Source
{
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;

    public class IntelligenceStance(IFightable owner)
        : StanceBase(owner, effect: new StanceActivationEffect([],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Intelligence, 15)]), Stance.Intelligence)
    {
    }
}
