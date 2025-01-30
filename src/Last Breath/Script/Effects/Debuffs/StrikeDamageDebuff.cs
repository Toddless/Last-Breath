namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;
    using Playground.Script.Passives;

    public class StrikeDamageDebuff : AbstractEffect
    {
        public StrikeDamageDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Parameter = Parameter.StrikeDamage;
            EffectType = EffectType.Debuff;
        }
    }
}
