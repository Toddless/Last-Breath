namespace Playground.Script.Passives.Buffs
{
    using Playground.Script.Enums;
    using Playground.Script.Passives;
    public class CriticalStrikeChanceBuff : AbstractEffect
    {
        public CriticalStrikeChanceBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Parameter.CriticalStrikeChance;
            EffectType = EffectType.Buff;
        }
    }
}
