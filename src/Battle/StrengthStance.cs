namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class StrengthStance(IEntity owner)
        : StanceBase(owner, effect: new StanceActivationEffect(), Stance.Strength);
}
