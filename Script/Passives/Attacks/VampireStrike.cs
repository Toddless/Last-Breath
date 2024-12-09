namespace Playground.Script.Passives.Attacks
{
    public partial class VampireStrike : Ability
    {
        private float _leachPercentage = 0.1f;

        public VampireStrike()
        {
            HaveISomethinToApplyAfterAttack = true;
        }

        public float LeechPercentage
        {
            get => _leachPercentage;
            set => _leachPercentage = value;
        }


        public override void ApplyAfterAttack(AttackComponent? attack, HealthComponent? health)
        {

        }

        public override void ApplyAfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {

        }


        public override void ApplyBeforAttack(AttackComponent? attack, HealthComponent? health)
        {

        }
    }
}
