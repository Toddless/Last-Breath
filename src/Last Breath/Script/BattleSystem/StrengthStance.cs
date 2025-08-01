namespace LastBreath.Script.BattleSystem
{
    using Contracts.Enums;
    using LastBreath.Script;

    public class StrengthStance : StanceBase
    {
        public StrengthStance(ICharacter owner) : base(owner, new Fury(), effect: new StanceActivationEffect(), Stance.Strength)
        {
            StanceSkillManager = new(this);
        }
    }
}
