namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class DoubleStrike : Ability, IDealDamage
    {
        public DoubleStrike()
        {
        }

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();

        public override void BuffAttacks(AttackComponent? attack = null) => throw new System.NotImplementedException();
    }
}
