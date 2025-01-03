namespace Playground.Script.Passives.Debuffs
{
    using Playground.Script.Enums;

    public class BoneBreaker : BaseEffect
    {
        private float _strengthReduceEffect = 0.95f;

        public BoneBreaker(EffectType type, int duration, string description) : base(type, duration, description)
        {
            Duration = 3;
            Description = $"Next strike receives a crushing effect. This effect reduces the maximum strength by 5%.";
        }

        public override void Activate()
        {

        }
    }
}
