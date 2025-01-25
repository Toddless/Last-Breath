using Playground.Script.Enums;

namespace Playground.Script.Passives
{
    public abstract class AbstractBuff : AbstractEffect
    {
        protected AbstractBuff(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            EffectType = EffectType.Buff;
        }
    }
}
