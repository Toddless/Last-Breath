namespace Playground.Script.BattleSystem
{
    using Playground.Script;
    using Playground.Script.Enums;

    public class StrengthStance : StanceBase
    {
        public StrengthStance(ICharacter owner) : base(owner, new Fury(), effect: new StanceActivationEffect(), Stance.Strength)
        {
            StanceSkillManager = new(this);
        }
    }
}
