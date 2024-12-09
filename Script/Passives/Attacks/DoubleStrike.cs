namespace Playground.Script.Passives.Attacks
{

    public partial class DoubleStrike : Ability
    {
        public DoubleStrike()
        {
        }

        public override void ApplyAfterAttack(AttackComponent? attack, HealthComponent? health) => throw new System.NotImplementedException();
        public override void ApplyAfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();
        public override void ApplyBeforAttack(AttackComponent? attack, HealthComponent? health) => throw new System.NotImplementedException();
    }
}
