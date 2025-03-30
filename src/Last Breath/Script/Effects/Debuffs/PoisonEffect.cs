namespace Playground.Script.Effects.Debuffs
{
    public class PoisonEffect : AbstractEffect
    {
        public PoisonEffect(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            EffectType = Enums.EffectType.Poison;
        }
    }
}
