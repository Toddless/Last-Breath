namespace Battle.Source
{
    using Abilities;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;

    public class IntelligenceStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([],
                [new Modifier(ModifierValueType.Flat, EntityParameter.Intelligence, 15)]), Stance.Intelligence,
            [
                new Fireball([], 3, 150, 0.07f, 50, [], [], []),
                new ManaDevour([], 75, 3, 0.5f, 0.02f, [], [], [])
            ])
    {
    }
}
