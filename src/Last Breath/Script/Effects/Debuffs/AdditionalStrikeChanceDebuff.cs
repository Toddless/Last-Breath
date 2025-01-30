namespace Playground.Script.Passives.Debuffs
{
    public class AdditionalStrikeChanceDebuff : AbstractEffect
    {
        public AdditionalStrikeChanceDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Enums.Parameter.AdditionalStrikeChance;
            EffectType = Enums.EffectType.Debuff;
        }
    }
}
