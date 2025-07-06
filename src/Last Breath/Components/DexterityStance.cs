namespace Playground.Components
{
    using Playground.Script;

    public class DexterityStance : StanceBase
    {
        public DexterityStance(ICharacter owner) : base(owner, new ComboPoints(), effect: new StanceActivationEffect())
        {
        }

        protected override void HandleAttackSucceed(ICharacter target)
        {
            base.HandleAttackSucceed(target);
        }

        public override void OnActivate() => ActivationEffect.OnActivate(Owner);

        public override void OnDeactivate() => ActivationEffect.OnDeactivate(Owner);
    }
}
