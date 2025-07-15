namespace Playground.Script.BattleSystem
{
    using Playground.Script;

    public class IntelligenceStance : StanceBase
    {
        public IntelligenceStance(ICharacter owner) : base(owner, resource: new Mana(), effect: new StanceActivationEffect())
        {
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
