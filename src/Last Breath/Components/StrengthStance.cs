namespace Playground.Components
{
    using Godot;
    using Playground.Script;

    public class StrengthStance : StanceBase
    {
        private StanceActivationEffect _effect;

        public StrengthStance(ICharacter owner) : base(owner, new Fury())
        {
            _effect = new();
        }

        protected override void PerformActionWhenAttackReceived(AttackContext context)
        {
            base.PerformActionWhenAttackReceived(context);
            OnAttack(context.Attacker);
        }

        public override void OnActivate()
        {
            _effect.OnActivate(Owner);
        }

        public override void OnDeactivate()
        {
            _effect.OnDeactivate(Owner);
        }
    }
}
