namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;
    public class CriticalStrikeChanceDebuff : AbstractDebuff
    {
        public CriticalStrikeChanceDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.CriticalStrikeChance;
        }
    }
}
