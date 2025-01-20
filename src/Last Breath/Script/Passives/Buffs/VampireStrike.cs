namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class VampireStrike : Ability<AttackComponent>, ICanLeech, ICanBuffAttack
    {
        private float _leachPercentage = 0.1f;

        public VampireStrike(AttackComponent component) : base(component)
        {
        }

        public override void ActivateAbility(AttackComponent? component) => component.Leech += _leachPercentage;
        public override void AfterBuffEnds(AttackComponent? component) => component.Leech -= _leachPercentage;
        public override void EffectAfterAttack(AttackComponent? component) => throw new System.NotImplementedException();
    }
}
