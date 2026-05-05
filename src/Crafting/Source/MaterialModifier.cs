namespace Crafting.Source
{
    using Godot;
    using Core.Enums;
    using Core.Modifiers;

    [GlobalClass]
    public partial class MaterialModifier : Resource, IModifier
    {
        private float _value;
        private bool _initialized = false;
        [Export] public EntityParameter EntityParameter { get; private set; }
        [Export] public ModifierValueType ModifierValueType { get; private set; }
        [Export] public float BaseValue { get; private set; }
        [Export] public float Weight { get; set; }

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

        public MaterialModifier(EntityParameter entityParameter, ModifierValueType valueType, float value, float weight)
        {
            EntityParameter = entityParameter;
            ModifierValueType = valueType;
            BaseValue = value;
            Weight = weight;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not IModifier other) return false;
            return EntityParameter == other.EntityParameter && ModifierValueType == other.ModifierValueType;
        }

        public override int GetHashCode() => System.HashCode.Combine(EntityParameter, ModifierValueType);
    }
}
