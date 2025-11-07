namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class StrengthStance : StanceBase
    {
        public StrengthStance(IEntity owner) : base(owner, new Fury(), effect: new StanceActivationEffect(), Stance.Strength)
        {
           // StanceSkillComponent = new StanceSkillComponent(this);
        }
    }
}
