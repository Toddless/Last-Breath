namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;
    public class CriticalStrikeDamageDebuff : AbstractEffect
    {
        public CriticalStrikeDamageDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Parameter.CriticalStrikeDamage;
            EffectType = EffectType.Debuff;
        }
    }
}
