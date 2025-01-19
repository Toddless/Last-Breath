namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class VampireStrike : Ability<HealthComponent>, ICanLeech, ICanBuffAttack
    {
        private float _leachPercentage = 0.1f;

        public VampireStrike(HealthComponent component) : base(component)
        {
        }

        public override void ActivateAbility(HealthComponent? component) => throw new System.NotImplementedException();
        public override void AfterBuffEnds(HealthComponent? component) => throw new System.NotImplementedException();
        public override void EffectAfterAttack(HealthComponent? component) => throw new System.NotImplementedException();
    }
}
