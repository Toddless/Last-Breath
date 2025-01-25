namespace Playground.Script.Passives.Buffs
{
    using Playground.Script.Enums;
    using Playground.Script.Passives;
    public class CriticalStrikeDamageBuff : AbstractBuff
    {
        public CriticalStrikeDamageBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.CriticalStrikeDamage;
        }
    }
}
