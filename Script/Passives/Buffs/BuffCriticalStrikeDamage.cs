namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeDamage : Ability, ICanBuffAttack
    {
        private readonly float _criticalStrikeDamageBonus = 0.2f;

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => attack.CriticalStrikeDamage -= _criticalStrikeDamageBonus;

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = default)
        {
            attack.CriticalStrikeDamage += _criticalStrikeDamageBonus;
        }
        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();
    }
}
