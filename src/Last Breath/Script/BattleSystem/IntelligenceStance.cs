namespace Playground.Script.BattleSystem
{
    using Playground.Script;
    using Playground.Script.Enums;

    public class IntelligenceStance : StanceBase
    {
        public IntelligenceStance(ICharacter owner) : base(owner, resource: new Mana(), effect: new StanceActivationEffect(), Stance.Intelligence)
        {
            StanceSkillManager = new(this);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            ActivationEffect.OnActivate(Owner);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            ActivationEffect.OnDeactivate(Owner);
        }
    }
}
