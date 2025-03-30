namespace Playground.Script.Effects.Buffs
{
    using Playground.Script.Enums;

    public class CriticalStrikeDamageBuff : AbstractEffect
    {
        public CriticalStrikeDamageBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Parameter.CriticalStrikeDamage;
            EffectType = EffectType.Buff;
        }
    }
}
