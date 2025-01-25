namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;
    using Playground.Script.Passives;

    public class StrikeDamageDebuff : AbstractDebuff
    {
        public StrikeDamageDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.StrikeDamage;
        }
    }
}
