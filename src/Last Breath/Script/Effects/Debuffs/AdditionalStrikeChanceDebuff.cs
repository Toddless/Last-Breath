namespace Playground.Script.Passives.Debuffs
{
    public class AdditionalStrikeChanceDebuff : AbstractEffect
    {
        public AdditionalStrikeChanceDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Enums.Stats.AdditionalStrikeChance;
            EffectType = Enums.EffectType.Debuff;
        }
    }
}
