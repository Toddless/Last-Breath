namespace Playground.Script.Passives
{
    using Playground.Script.Enums;
    public abstract class AbstractEffect(string name, string desc, float modifier, int duration) : IEffect
    {
        private string _name = name;
        private string _description = desc;
        private float _modifier = modifier;
        private EffectType _effectType;
        private Stats _stat;
        private int _duration = duration;

        public Stats Stat
        {
            get => _stat;
            protected set => _stat = value;
        }

        public string Name => _name;
        public string Description => _description;
        public float Modifier => _modifier;
        public int Duration => _duration;

        public EffectType EffectType
        {
            get => _effectType;
            protected set => _effectType = value;
        }
    }
}
