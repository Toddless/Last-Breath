namespace Playground.Script.Passives.Attacks
{
    public partial class BuffAttack : Ability
    {
        private readonly float _additionalDamage = 1.3f;

        private float _baseMinDamageBeforBuff;
        private float _baseMaxDamageBeforBuff;


        public BuffAttack()
        {
        }

        public override void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default)
        {
            
          
        }

        public override void ApplyAfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if (attack == null)
            {
                return;
            }
            attack.BaseMinDamage = _baseMinDamageBeforBuff;
            attack.BaseMaxDamage = _baseMaxDamageBeforBuff;

            _baseMinDamageBeforBuff = default;
            _baseMaxDamageBeforBuff = default;
        }

        public override void ApplyBeforAttack(AttackComponent? attack, HealthComponent? health)
        {
            if(attack == null)
            {
                return;
            }
            _baseMinDamageBeforBuff = attack.BaseMinDamage;
            _baseMaxDamageBeforBuff = attack.BaseMaxDamage;

            attack.BaseMinDamage *= _additionalDamage;
            attack.BaseMaxDamage *= _additionalDamage;
        }
    }
}
