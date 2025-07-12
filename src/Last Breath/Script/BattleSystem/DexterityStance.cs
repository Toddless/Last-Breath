namespace Playground.Script.BattleSystem
{
    using Playground.Components;
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

        protected override void PerformActionWhenAttackReceived(AttackContext context)
        {
            OnAttack(context.Attacker);
        }

        public override void OnActivate() => ActivationEffect.OnActivate(Owner);

        public override void OnDeactivate() => ActivationEffect.OnDeactivate(Owner);
    }
}
