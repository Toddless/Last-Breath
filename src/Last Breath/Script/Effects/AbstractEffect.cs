namespace Playground.Script.Effects
{
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;

    public abstract class AbstractEffect(string name, string desc, float modifier, int duration) : IEffect
    {
        private string _name = name;
        private string _description = desc;
        private float _modifier = modifier;
        private int _duration = duration;
        private EffectType _effectType;
        private Parameter _stat;

        public Parameter Parameter
        {
            get => _stat;
            protected set => _stat = value;
        }

        public string Name => _name;
        public string Description => _description;
        public float Modifier => _modifier;
        public int Duration
        {
            get => _duration;
            set => _duration = value;
        }

        public EffectType EffectType
        {
            get => _effectType;
            protected set => _effectType = value;
        }
    }
}
