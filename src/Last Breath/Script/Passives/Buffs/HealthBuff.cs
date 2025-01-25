namespace Playground.Script.Passives.Buffs
{
    using Playground.Script.Enums;

    public class HealthBuff: AbstractBuff
    {
        public HealthBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.Health;
        }
    }
}
