namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;

    public class IntelligenceStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect(
                [],
                [new Modifier(ModifierType.Flat, EntityParameter.Intelligence, 15)]),
            Stance.Intelligence)
    {
    }
}
