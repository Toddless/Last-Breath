namespace Playground.Script.Passives.Attacks
{

    public partial class BuffCriticalStrikeChance : Ability
    {
        private readonly float _criticalStrikeChanceBonus = 0.1f;


        public override void ApplyAfterAttack(AttackComponent? attack = null, HealthComponent? health = null)=> throw new System.NotImplementedException();

        public override void ApplyAfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => attack.CriticalStrikeChance -= _criticalStrikeChanceBonus;

        public override void ApplyBeforAttack(AttackComponent? attack, HealthComponent? health) => attack.CriticalStrikeChance += _criticalStrikeChanceBonus;
    }
}
