namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class Badabooom : Ability<AttackComponent, BaseEnemy>, ICanDealDamage
    {
        private float _damageMultiplier = 2;

        private float _minBeforBuff;
        private float _maxBeforBuff;

        public override void ActivateAbility(AttackComponent? component)
        {
            _minBeforBuff = component!.BaseMinDamage;
            _maxBeforBuff = component.BaseMaxDamage;

            component.BaseMinDamage *= _damageMultiplier;
            component.BaseMaxDamage *= _damageMultiplier;
        }

        public override void SetTargetCharacter(BaseEnemy? target) => throw new System.NotImplementedException();
    }
}
