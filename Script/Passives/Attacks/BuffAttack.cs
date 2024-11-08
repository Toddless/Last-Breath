namespace Playground.Script.Passives.Attacks
{
    using Godot;

    public partial class BuffAttack() : Node, IPassivesAppliedBeforAttack, IPassivesAppliedAfterAttack
    {
        private readonly float _additionalCriticalStrikeChance = 1.05f;
        private readonly float _additionalCritStrikeDamage = 1.1f;
        private readonly float _additionalMinDamage = 1.3f;
        private readonly float _additionalMaxDamage = 1.3f;


        private float _minDamageBeforBuff;
        private float _maxDamageBeforBuff;
        private float _criticalStrikeDamageBeforeBuff;
        private float _criticalChanceBeforBuff;


        private int _cooldown = 4;

        public int Cooldown
        {
            get => _cooldown;
            set => _cooldown = value;
        }

        public void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default)
        {
            attack!.CriticalStrikeChance = _criticalStrikeDamageBeforeBuff;
            attack.CriticalStrikeDamage = _criticalChanceBeforBuff;
            attack.BaseMinDamage = _minDamageBeforBuff;
            attack.BaseMaxDamage = _maxDamageBeforBuff;
        }

        public void ApplyBeforeAttack(AttackComponent? attack)
        {
            _criticalStrikeDamageBeforeBuff = attack!.CriticalStrikeDamage;
            _criticalChanceBeforBuff = attack.CriticalStrikeChance;
            _minDamageBeforBuff = attack.BaseMinDamage;
            _maxDamageBeforBuff = attack.BaseMaxDamage;

            attack.CriticalStrikeChance *= _additionalCriticalStrikeChance;
            attack.CriticalStrikeDamage *= _additionalCritStrikeDamage;
            attack.BaseMinDamage *= _additionalMinDamage;
            attack.BaseMaxDamage *= _additionalMaxDamage;
        }
    }
}
