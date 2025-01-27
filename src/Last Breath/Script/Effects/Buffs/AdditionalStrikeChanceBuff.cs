using Playground.Script.Enums;

namespace Playground.Script.Passives.Buffs
{
    public class AdditionalStrikeChanceBuff : AbstractEffect
    {
        public AdditionalStrikeChanceBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Parameter.AdditionalStrikeChance;
            EffectType = EffectType.Buff;
        }
    }
}
