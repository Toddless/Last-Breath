namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;
    public class CriticalStrikeChanceDebuff : AbstractEffect
    {
        public CriticalStrikeChanceDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Parameter.CriticalStrikeChance;
            EffectType = EffectType.Debuff;
        }
    }
}
