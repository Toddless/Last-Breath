namespace Playground.Script.Passives
{
    using Playground.Script.Enums;

    public abstract class AbstractDebuff : AbstractEffect
    {
        protected AbstractDebuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            EffectType = EffectType.Debuff;
        }
    }
}
