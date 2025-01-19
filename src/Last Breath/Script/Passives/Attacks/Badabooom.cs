namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class Badabooom : Ability<AttackComponent>, ICanDealDamage
    {
        private float _damageMultiplier = 2;

        private float _minBeforBuff;
        private float _maxBeforBuff;

        public Badabooom(AttackComponent component) : base(component)
        {
        }

        public override void ActivateAbility(AttackComponent? component)
        {
            _minBeforBuff = component!.BaseMinDamage;
            _maxBeforBuff = component.BaseMaxDamage;

            component.BaseMinDamage *= _damageMultiplier;
            component.BaseMaxDamage *= _damageMultiplier;
        }

        public override void AfterBuffEnds(AttackComponent? component)
        {
            component!.BaseMinDamage = _minBeforBuff;
            component.BaseMaxDamage = _maxBeforBuff;
        }
        public override void EffectAfterAttack(AttackComponent? component) => throw new System.NotImplementedException();
    }
}
