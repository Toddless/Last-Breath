namespace Playground.Script.Passives.Attacks
{
    public class BuffAttack : AttackPassives
    {
        public override void OnActivated(AttackComponent attack)
        {
            attack.CriticalStrikeDamage += 0.2f;
            attack.CriticalStrikeChance *= 1.1f;
            attack.BaseMinDamage *= 1.2f;
            attack.BaseMaxDamage *= 1.2f;
        }
    }
}
