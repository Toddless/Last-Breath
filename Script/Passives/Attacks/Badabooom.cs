namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class Badabooom : Ability, ICanDealDamage
    {
        private float _damageMultiplier = 2;

        private float _minBeforBuff;
        private float _maxBeforBuff;

        public Badabooom()
        {
            Cooldown = 4;
        }

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = null)
        {
            _minBeforBuff = attack.BaseMinDamage;
            _maxBeforBuff = attack.BaseMaxDamage;

            attack.BaseMinDamage *= _damageMultiplier;
            attack.BaseMaxDamage *= _damageMultiplier;
        }

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {
            attack.BaseMinDamage = _minBeforBuff;
            attack.BaseMaxDamage = _maxBeforBuff;
        }

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null)
        {
           
        }
    }
}
