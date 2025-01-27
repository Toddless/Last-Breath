namespace Playground.Script.Passives.Debuffs
{
    internal class PoisonEffect : AbstractEffect
    {
        public PoisonEffect(string name, string desc, float modifier, int duration) : base(name, desc, modifier, duration)
        {
            EffectType = Enums.EffectType.Poison;
        }
    }
}
