namespace Crafting.Source
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;

    [GlobalClass]
    public partial class MaterialModifier : Resource, IMaterialModifier
    {
        private float _value;
        private bool _initialized = false;
        [Export] public Parameter Parameter { get; private set; }
        [Export] public ModifierType ModifierType { get; private set; }
        [Export] public float BaseValue { get; private set; }
        [Export] public float Weight { get; private set; }

        public float Value
        {
            get
            {
                if (!_initialized)
                {
                    _value = BaseValue;
                    _initialized = true;
                }
                return _value;
            }
            set => _value = value;
        }


        public MaterialModifier()
        {

        }

        public MaterialModifier(Parameter parameter, ModifierType type, float value, float weight)
        {
            Parameter = parameter;
            ModifierType = type;
            BaseValue = value;
            Weight = weight;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is not IModifier other) return false;
            return Parameter == other.Parameter && ModifierType == other.ModifierType;
        }

        public override int GetHashCode() => System.HashCode.Combine(Parameter, ModifierType);
    }
}
