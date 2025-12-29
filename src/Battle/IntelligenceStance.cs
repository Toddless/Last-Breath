namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Modifiers;
    using Source.Abilities;

    public class IntelligenceStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect([],
            [new Modifier(ModifierType.Flat, EntityParameter.Intelligence, 15)]), Stance.Intelligence,
            [new Fireball(3, 1, 100f, 0.07f, [], 50, 50, [], [], []),
            new ManaDevour([], 50, 75, 3, 0.5f, 0.02f, [],[],[])])
    {
    }
}
