namespace Playground.Script.BattleSystem
{
    using Playground.Components;
    using Playground.Script;

    public class StrengthStance : StanceBase
    {

        public StrengthStance(ICharacter owner) : base(owner, new Fury(), effect: new StanceActivationEffect())
        {
        }

        protected override void PerformActionWhenAttackReceived(AttackContext context)
        {
            base.PerformActionWhenAttackReceived(context);
            OnAttack(context.Attacker);
        }

        public override void OnActivate() => ActivationEffect.OnActivate(Owner);

        public override void OnDeactivate() => ActivationEffect.OnDeactivate(Owner);
    }
}
