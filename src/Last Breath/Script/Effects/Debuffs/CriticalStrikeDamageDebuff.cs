namespace Playground.Script.Effects.Debuffs
{
    using Playground.Script.Enums;

    public class CriticalStrikeDamageDebuff : AbstractEffect
    {
        public CriticalStrikeDamageDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Parameter.CriticalStrikeDamage;
            EffectType = EffectType.Debuff;
        }
    }
}
