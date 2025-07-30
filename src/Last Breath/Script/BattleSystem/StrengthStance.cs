namespace LastBreath.Script.BattleSystem
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

    public class StrengthStance : StanceBase
    {
        public StrengthStance(ICharacter owner) : base(owner, new Fury(), effect: new StanceActivationEffect(), Stance.Strength)
        {
            StanceSkillManager = new(this);
        }
    }
}
