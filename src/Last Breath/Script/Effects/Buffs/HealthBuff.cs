namespace Playground.Script.Passives.Buffs
{
    using Playground.Script.Enums;

    public class HealthBuff: AbstractEffect
    {
        public HealthBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Stats.Health;
            EffectType = EffectType.Buff;
        }
    }
}
