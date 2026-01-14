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
        [Export] public EntityParameter EntityParameter { get; private set; }
        [Export] public ModifierType ModifierType { get; private set; }
        [Export] public float BaseValue { get; private set; }
        [Export] public float Weight { get; private set; }

        public float Value
        {
            get
            {
                if (_initialized)
                    return _value;

                _value = BaseValue;
                _initialized = true;

                return _value;
            }
            set => _value = value;
        }


        public MaterialModifier()
        {
        }

        public MaterialModifier(EntityParameter entityParameter, ModifierType type, float value, float weight)
        {
            EntityParameter = entityParameter;
            ModifierType = type;
            BaseValue = value;
            Weight = weight;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not IModifier other) return false;
            return EntityParameter == other.EntityParameter && ModifierType == other.ModifierType;
        }

        public override int GetHashCode() => System.HashCode.Combine(EntityParameter, ModifierType);
    }
}
