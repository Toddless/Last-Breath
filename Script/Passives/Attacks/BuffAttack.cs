namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public partial class BuffAttack() : Passive, IAttackPassives
    {
        private readonly float _additionalCriticalStrikeChance = 1.05f;
        private readonly float _additionalCritStrikeDamage = 1.1f;
        private readonly float _additionalMinDamage = 1.3f;
        private readonly float _additionalMaxDamage = 1.3f;
       

        private float _minDamageBeforBuff;
        private float _maxDamageBeforBuff;
        private float _criticalStrikeDamageBeforeBuff;
        private float _criticalChanceBeforBuff;

        private int _buffLasts = 3;

        public void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default, float amount = default)
        {
            if(_buffLasts <= 0 && attack != null)
            {
                attack.CriticalStrikeChance = _criticalStrikeDamageBeforeBuff;
                attack.CriticalStrikeDamage = _criticalChanceBeforBuff;
                attack.BaseMinDamage = _minDamageBeforBuff;
                attack.BaseMaxDamage = _maxDamageBeforBuff;
                _buffLasts = 3;
            }
        }

        public void ApplyBeforeAttack(AttackComponent attack)
        {
            if(_buffLasts != 3)
            {
                _buffLasts--;
                return;
            }

            GD.Print($"Buff activated. Turns left: {_buffLasts}");
            _criticalStrikeDamageBeforeBuff = attack.CriticalStrikeDamage;
            _criticalChanceBeforBuff = attack.CriticalStrikeChance;
            _minDamageBeforBuff = attack.BaseMinDamage;
            _maxDamageBeforBuff = attack.BaseMaxDamage;

            attack.CriticalStrikeChance *= _additionalCriticalStrikeChance;
            attack.CriticalStrikeDamage *= _additionalCritStrikeDamage;
            attack.BaseMinDamage *= _additionalMinDamage;
            attack.BaseMaxDamage *= _additionalMaxDamage;
            _buffLasts--;
        }
    }
}
