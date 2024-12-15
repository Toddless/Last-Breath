namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class DoubleStrike : Ability, ICanDealDamage
    {
        public DoubleStrike()
        {
        }

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = default) => throw new System.NotImplementedException();
        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();
    }
}
