namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;

    public class HealthDebuf : AbstractDebuff
    {
        public HealthDebuf(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.Health;
        }
    }
}
