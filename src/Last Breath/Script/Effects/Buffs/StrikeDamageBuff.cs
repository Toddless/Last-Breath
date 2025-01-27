namespace Playground.Script.Passives.Buffs
{
    using Playground.Script.Enums;
    using Playground.Script.Passives;
    public class StrikeDamageBuff : AbstractEffect
    {
        public StrikeDamageBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            Stat = Parameter.StrikeDamage;
            EffectType = EffectType.Buff;
        }
    }
}
