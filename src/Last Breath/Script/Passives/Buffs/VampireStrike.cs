namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class VampireStrike : Ability<AttackComponent, BaseEnemy>, ICanLeech, ICanBuffAttack
    {
        private float _leachPercentage = 0.1f;

        public override void ActivateAbility(AttackComponent? component) => component.Leech += _leachPercentage;
        public override void SetTargetCharacter(BaseEnemy? target) => throw new System.NotImplementedException();
    }
}
