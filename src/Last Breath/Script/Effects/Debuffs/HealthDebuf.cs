namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;

    public class HealthDebuf : AbstractEffect
    {
        public HealthDebuf(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Parameter.Health;
            EffectType = EffectType.Debuff;
        }
    }
}
