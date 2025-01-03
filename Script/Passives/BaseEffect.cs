namespace Playground.Script.Passives
{
    using Godot;
    using Playground.Script.Enums;

    public abstract class BaseEffect
    {
        private Texture2D? _icon;
        private int _duration;
        private string? _description;
        private EffectType _effectType;

        public Texture2D? Icon
        {
            get => _icon;
            set => _icon = value;
        }

        public EffectType EffectType => _effectType;

        public int Duration
        {
            get => _duration;
            set => _duration = value;
        }

        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        public bool IsExpired => _duration <= 0;

        protected BaseEffect(EffectType type, int duration, string description)
        {
            _effectType = type;
            _duration = duration;
            _description = description;
        }

        public abstract void Activate();
    }
}
