namespace Playground.Script.Effects.Buffs
{
    using Playground.Script.Enums;

    public class AdditionalStrikeChanceBuff : AbstractEffect
    {
        public AdditionalStrikeChanceBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Parameter.AdditionalStrikeChance;
            EffectType = EffectType.Buff;
        }
    }
}
