namespace Playground.Script.Effects.Debuffs
{
    using Playground.Script.Passives;
    public class BleedEffect : AbstractEffect
    {
        public BleedEffect(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            EffectType = Enums.EffectType.Bleeding;
        }
    }
}
