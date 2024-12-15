namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeChance : Ability, ICanBuffAttack
    {
        private readonly float _criticalStrikeChanceBonus = 0.1f;


        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => attack.CriticalStrikeChance -= _criticalStrikeChanceBonus;

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = default)
        {
            attack.CriticalStrikeChance += _criticalStrikeChanceBonus;
        }

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new System.NotImplementedException();
    }
}
