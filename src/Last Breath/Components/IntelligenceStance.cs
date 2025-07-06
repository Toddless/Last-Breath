namespace Playground.Components
{
    using Playground.Script;

    public class IntelligenceStance : StanceBase
    {
        public IntelligenceStance(ICharacter owner) : base(owner, resource: new Mana(), effect: new StanceActivationEffect())
        {
        }

        public override void OnActivate() => ActivationEffect.OnActivate(Owner);
        public override void OnDeactivate() => ActivationEffect.OnDeactivate(Owner);
    }
}
