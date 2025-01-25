namespace Playground.Script.Passives.Debuffs
{
    public class AdditionalStrikeChanceDebuff : AbstractDebuff
    {
        public AdditionalStrikeChanceDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Enums.Stats.AdditionalStrikeChance;
        }
    }
}
