namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using Core.Interfaces;

    public class StrengthStance : StanceBase
    {
        public StrengthStance(ICharacter owner) : base(owner, new Fury(), effect: new StanceActivationEffect(), Stance.Strength)
        {
            StanceSkillComponent = new StanceSkillComponent(this);
        }
    }
}
