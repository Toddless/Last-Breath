namespace Playground.Script.Passives.Buffs
{
    public class AdditionalStrikeChanceBuff : AbstractBuff
    {
        public AdditionalStrikeChanceBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Enums.Stats.AdditionalStrikeChance;
        }
    }
}
