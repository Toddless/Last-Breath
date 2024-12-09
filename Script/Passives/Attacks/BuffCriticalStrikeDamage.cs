namespace Playground.Script.Passives.Attacks
{

    public partial class BuffCriticalStrikeDamage : Ability
    {
        private readonly float _criticalStrikeDamageBonus = 0.2f;

        public override void ApplyAfterAttack(AttackComponent? attack = null, HealthComponent? health = null)
        {
        }

        public override void ApplyAfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => attack.CriticalStrikeDamage -= _criticalStrikeDamageBonus;

        public override void ApplyBeforAttack(AttackComponent? attack, HealthComponent? health) => attack.CriticalStrikeDamage += _criticalStrikeDamageBonus;

    }
}
