namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class BuffAttack : Ability, ICanBuffAttack
    {
        private readonly float _additionalDamage = 1.3f;

        private float _baseMinDamageBeforBuff;
        private float _baseMaxDamageBeforBuff;

        public BuffAttack()
        {
        }

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = default)
        {

            if (attack == null)
            {
                return;
            }
            _baseMinDamageBeforBuff = attack.BaseMinDamage;
            _baseMaxDamageBeforBuff = attack.BaseMaxDamage;

            attack.BaseMinDamage *= _additionalDamage;
            attack.BaseMaxDamage *= _additionalDamage;
        }

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if (attack == null)
            {
                return;
            }
            attack.BaseMinDamage = _baseMinDamageBeforBuff;
            attack.BaseMaxDamage = _baseMaxDamageBeforBuff;
        }

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();
    }
}
