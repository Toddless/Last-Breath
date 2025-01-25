namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;
    public class CriticalStrikeDamageDebuff : AbstractDebuff
    {
        public CriticalStrikeDamageDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.CriticalStrikeDamage;
        }
    }
}
